// Leaflet flight map using embedded JSON (script#flightMapData)
(function(){
    const mapEl=document.getElementById('flightMap');
    if(!mapEl||!window.L){console.warn('[flight-map] map element or Leaflet missing');return;}

    const cssVar=n=>getComputedStyle(document.documentElement).getPropertyValue(n).trim();
    const colorPast=cssVar('--flight-map-past-line')||'#1565C0';
    const colorUpcoming=cssVar('--flight-map-upcoming-marker')||'#FFB300';
    const colorHighlight=cssVar('--flight-map-highlight')||'#ff4081';

    function readFlights(){
        const el=document.getElementById('flightMapData');
        if(!el){console.warn('[flight-map] #flightMapData not found');return [];}    
        const raw=el.textContent||'[]';
        try{const parsed=JSON.parse(raw);return parsed;}catch(e){console.warn('[flight-map] JSON parse error',e);return [];}
    }

    // Use worldCopyJump for seamless horizontal panning; Canvas renderer for better path perf
    const map=L.map('flightMap',{center:[20,0],zoom:2,worldCopyJump:true});
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',{attribution:'&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'}).addTo(map);
    const renderer=L.canvas({padding:0.2});

    const layers={past:L.layerGroup().addTo(map),upcoming:L.layerGroup().addTo(map)};
    const flightIndex=new Map();
    let lastFilterKeys=[];
    const highlight={
        past:{color:colorHighlight,weight:5,opacity:0.95},
        upcoming:{radius:9,color:colorHighlight,fillColor:colorHighlight,fillOpacity:1,weight:2}
    };
    const normal={
        past:{color:colorPast,weight:3,opacity:0.75},
        upcoming:{radius:7,color:colorUpcoming,fillColor:colorUpcoming,fillOpacity:0.9,weight:2},
        upcomingPath:{color:colorUpcoming,weight:2.25,opacity:0.65,dashArray:'6,6'}
    };

    // Ensure the map resizes to its container height; handle flex/layout changes.
    function scheduleInvalidate(){
        try{ map.invalidateSize(false); }catch(_){ }
        if('requestAnimationFrame' in window){ requestAnimationFrame(()=>{ try{ map.invalidateSize(false);}catch(_){ } }); }
        [16,100,300,800,1500].forEach(ms=>setTimeout(()=>{ try{ map.invalidateSize(false);}catch(_){ } }, ms));
    }
    if('ResizeObserver' in window){
        try{ new ResizeObserver(()=>scheduleInvalidate()).observe(mapEl); }catch(_){ }
    }
    window.addEventListener('resize', scheduleInvalidate);
    window.addEventListener('load', scheduleInvalidate);
    document.addEventListener('visibilitychange',()=>{ if(document.visibilityState==='visible') scheduleInvalidate(); });
    window.addEventListener('flightMap:invalidate', scheduleInvalidate);

    function render(){
        layers.past.clearLayers();layers.upcoming.clearLayers();flightIndex.clear();
        const flights=readFlights();
        if(!flights.length){console.log('[flight-map] no flights');return;}
        const polys=[];const markers=[];
        // Utility: create a geodesic (great-circle) arc as a polyline between two lat/lon points
        // Ensures continuity across the antimeridian by unwrapping longitudes.
        function createGeodesic(from, to, style){
            // Haversine-based interpolation along great-circle
            const lat1=from[0]*Math.PI/180, lon1=from[1]*Math.PI/180;
            const lat2=to[0]*Math.PI/180, lon2=to[1]*Math.PI/180;
            const d=2*Math.asin(Math.sqrt(
                Math.sin((lat2-lat1)/2)**2 + Math.cos(lat1)*Math.cos(lat2)*Math.sin((lon2-lon1)/2)**2
            )) || 0;
            const segments=Math.max(8, Math.min(128, Math.ceil(d*30))); // more distance -> more segments
            if(d===0){ return L.polyline([from,to], {...style, renderer, noClip:true}); }
            const coords=[];
            for(let i=0;i<=segments;i++){
                const f=i/segments;
                // Spherical linear interpolation
                const A=Math.sin((1-f)*d)/Math.sin(d);
                const B=Math.sin(f*d)/Math.sin(d);
                const x=A*Math.cos(lat1)*Math.cos(lon1)+B*Math.cos(lat2)*Math.cos(lon2);
                const y=A*Math.cos(lat1)*Math.sin(lon1)+B*Math.cos(lat2)*Math.sin(lon2);
                const z=A*Math.sin(lat1)+B*Math.sin(lat2);
                const lat=Math.atan2(z, Math.sqrt(x*x+y*y));
                const lon=Math.atan2(y,x);
                coords.push([lat*180/Math.PI, lon*180/Math.PI]);
            }
            // Unwrap longitudes to avoid short straight line across the dateline
            let prevLon=coords[0][1];
            let offset=0;
            const unwrapped=coords.map(([la,lo],idx)=>{
                if(idx===0){ prevLon=lo; return [la, lo]; }
                let delta=lo - prevLon;
                if(delta > 180) { offset -= 360; }
                else if(delta < -180) { offset += 360; }
                const adjLon=lo + offset;
                prevLon=lo;
                return [la, adjLon];
            });
            return L.polyline(unwrapped, {...style, renderer, noClip:true});
        }

        flights.forEach(f=>{
            const depOk=typeof f.departureLat==='number'&&typeof f.departureLon==='number';
            const arrOk=typeof f.arrivalLat==='number'&&typeof f.arrivalLon==='number';
            const key=`${f.flightNumber} ${f.departureAirportCode}->${f.arrivalAirportCode}`;
            if(f.isUpcoming){
                if(depOk){
                    const m=L.circleMarker([f.departureLat,f.departureLon],normal.upcoming).addTo(layers.upcoming);
                    m.bindPopup(`<strong>${f.flightNumber}</strong><br/>Departs for ${f.arrivalAirportCode}<br/>${new Date(f.departureTimeUtc).toUTCString()}`);
                    markers.push(m);
                    flightIndex.set(key,{flight:f,type:'upcoming',layer:m});

                    // Draw planned path if arrival is known
                    if(arrOk){
                        const from=[f.departureLat,f.departureLon];
                        const to=[f.arrivalLat,f.arrivalLon];
                        const pl=createGeodesic(from,to,normal.upcomingPath).addTo(layers.upcoming);
                        polys.push(pl);
                    }
                }
            }
            else if(depOk&&arrOk){
                const from=[f.departureLat,f.departureLon];
                const to=[f.arrivalLat,f.arrivalLon];
        const pl=createGeodesic(from,to,normal.past).addTo(layers.past);
                polys.push(pl);
                flightIndex.set(key,{flight:f,type:'past',layer:pl});
            }
        });
    let bounds=null;polys.forEach(pl=>bounds=bounds?bounds.extend(pl.getBounds()):pl.getBounds());markers.forEach(m=>{const ll=m.getLatLng();bounds=bounds?bounds.extend(ll):L.latLngBounds(ll,ll);});if(bounds)map.fitBounds(bounds.pad(0.15));
    scheduleInvalidate();
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
    scheduleInvalidate();
    window.flightMap={reload:render,zoomToSelection:window.flightMapZoomToSelection,zoomToFiltered:window.flightMapZoomToFiltered};
})();
