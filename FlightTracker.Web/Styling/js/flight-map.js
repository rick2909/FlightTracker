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
        try{const parsed=JSON.parse(raw);console.log('[flight-map] parsed flights',parsed.length);return parsed;}catch(e){console.warn('[flight-map] JSON parse error',e);return [];}    
    }

    const map=L.map('flightMap',{center:[20,0],zoom:2,worldCopyJump:true});
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',{attribution:'&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'}).addTo(map);

    const layers={past:L.layerGroup().addTo(map),upcoming:L.layerGroup().addTo(map)};

    function render(){
        layers.past.clearLayers();layers.upcoming.clearLayers();
        const flights=readFlights();
        if(!flights.length){console.log('[flight-map] no flights');return;}
        const polys=[];const markers=[];
        flights.forEach(f=>{
            const depOk=typeof f.departureLat==='number'&&typeof f.departureLon==='number';
            const arrOk=typeof f.arrivalLat==='number'&&typeof f.arrivalLon==='number';
            if(f.isUpcoming){
                if(depOk){
                    const m=L.circleMarker([f.departureLat,f.departureLon],{radius:7,color:colorUpcoming,fillColor:colorUpcoming,fillOpacity:0.9,weight:2}).addTo(layers.upcoming);
                    m.bindPopup(`<strong>${f.flightNumber}</strong><br/>Departs ${f.departureAirportCode}<br/>${new Date(f.departureTimeUtc).toUTCString()}`);
                    markers.push(m);
                }
            }else if(depOk&&arrOk){
                const pl=L.polyline([[f.departureLat,f.departureLon],[f.arrivalLat,f.arrivalLon]],{color:colorPast,weight:3,opacity:0.75}).addTo(layers.past);
                polys.push(pl);
            }
        });
        let bounds=null;polys.forEach(pl=>bounds=bounds?bounds.extend(pl.getBounds()):pl.getBounds());markers.forEach(m=>{const ll=m.getLatLng();bounds=bounds?bounds.extend(ll):L.latLngBounds(ll,ll);});if(bounds)map.fitBounds(bounds.pad(0.15));
    }

    const search=document.getElementById('flightSearch');
    if(search){search.addEventListener('input',()=>{render();});}

    render();
    window.flightMap={reload:render};
})();
