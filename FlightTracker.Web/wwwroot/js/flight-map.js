// Leaflet flight map using embedded JSON (script#flightMapData)
(function(){
    const mapEl=document.getElementById('flightMap');
    if(!mapEl||!window.L){console.warn('[flight-map] map element or Leaflet missing');return;}

    const cssVar=n=>getComputedStyle(document.documentElement).getPropertyValue(n).trim();
    const colorPast=cssVar('--flight-map-past-line')||'#1565C0';
    const colorUpcoming=cssVar('--flight-map-upcoming-marker')||'#FFB300';

    function readFlights(){
        const el=document.getElementById('flightMapData');
        if(!el){console.warn('[flight-map] #flightMapData not found');return [];}    
        const raw=el.textContent||'[]';
        try{const parsed=JSON.parse(raw);return parsed;}catch(e){console.warn('[flight-map] JSON parse error',e);return [];}
    }

    const map=L.map('flightMap',{center:[20,0],zoom:2,worldCopyJump:true});
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',{attribution:'&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'}).addTo(map);

    const layers={past:L.layerGroup().addTo(map),upcoming:L.layerGroup().addTo(map)};
    const flightIndex=new Map();
    let lastFilterKeys=[];
    const highlight={
        past:{color:'#ff4081',weight:5,opacity:0.95},
        upcoming:{radius:9,color:'#ff4081',fillColor:'#ff4081',fillOpacity:1,weight:2}
    };
    const normal={
        past:{color:colorPast,weight:3,opacity:0.75},
        upcoming:{radius:7,color:colorUpcoming,fillColor:colorUpcoming,fillOpacity:0.9,weight:2}
    };

    function render(){
        layers.past.clearLayers();layers.upcoming.clearLayers();flightIndex.clear();
        const flights=readFlights();
        if(!flights.length){console.log('[flight-map] no flights');return;}
        const polys=[];const markers=[];
        flights.forEach(f=>{
            const depOk=typeof f.departureLat==='number'&&typeof f.departureLon==='number';
            const arrOk=typeof f.arrivalLat==='number'&&typeof f.arrivalLon==='number';
            const key=`${f.flightNumber} ${f.departureAirportCode}->${f.arrivalAirportCode}`;
            if(f.isUpcoming){ if(depOk){ const m=L.circleMarker([f.departureLat,f.departureLon],normal.upcoming).addTo(layers.upcoming); m.bindPopup(`<strong>${f.flightNumber}</strong><br/>Departs ${f.departureAirportCode}<br/>${new Date(f.departureTimeUtc).toUTCString()}`); markers.push(m); flightIndex.set(key,{flight:f,type:'upcoming',layer:m}); } }
            else if(depOk&&arrOk){ const pl=L.polyline([[f.departureLat,f.departureLon],[f.arrivalLat,f.arrivalLon]],normal.past).addTo(layers.past); polys.push(pl); flightIndex.set(key,{flight:f,type:'past',layer:pl}); }
        });
        let bounds=null;polys.forEach(pl=>bounds=bounds?bounds.extend(pl.getBounds()):pl.getBounds());markers.forEach(m=>{const ll=m.getLatLng();bounds=bounds?bounds.extend(ll):L.latLngBounds(ll,ll);});if(bounds)map.fitBounds(bounds.pad(0.15));
    }

    window.flightMapClearFilter=function(){};
    window.flightMapFilter=function(term){
        if(lastFilterKeys.length){ lastFilterKeys.forEach(k=>{ const e=flightIndex.get(k); if(!e) return; if(e.type==='past'){ e.layer.setStyle(normal.past);} else { e.layer.setStyle(normal.upcoming);} }); }
        if(!term){ lastFilterKeys=[]; return; }
        const termLc=term.toLowerCase();
        lastFilterKeys=Array.from(flightIndex.keys()).filter(k=>k.toLowerCase().includes(termLc));
        lastFilterKeys.forEach(k=>{ const e=flightIndex.get(k); if(!e)return; if(e.type==='past'){ e.layer.setStyle(highlight.past);} else { e.layer.setStyle(highlight.upcoming);} });
    };
    window.flightMapZoomToSelection=function(key){ const e=flightIndex.get(key); if(!e)return; if(e.type==='past'){ map.fitBounds(e.layer.getBounds().pad(0.3)); } else { const ll=e.layer.getLatLng(); map.setView(ll, Math.max(map.getZoom(),6)); e.layer.openPopup(); } };
    window.flightMapZoomToFiltered=function(){ if(!lastFilterKeys.length)return; let b=null; lastFilterKeys.forEach(k=>{ const e=flightIndex.get(k); if(!e)return; if(e.type==='past'){ b=b?b.extend(e.layer.getBounds()):e.layer.getBounds(); } else { const ll=e.layer.getLatLng(); b=b?b.extend(ll):L.latLngBounds(ll,ll); } }); if(b) map.fitBounds(b.pad(0.2)); };

    render();
    window.flightMap={reload:render,zoomToSelection:window.flightMapZoomToSelection,zoomToFiltered:window.flightMapZoomToFiltered};
})();
