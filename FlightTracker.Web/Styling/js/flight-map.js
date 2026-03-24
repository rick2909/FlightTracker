// Leaflet flight map using embedded JSON in script[type="application/json"]
(function(){
    let map=null;
    let mapEl=null;
    let renderer=null;
    let layers={past:null,upcoming:null};
    const flightIndex=new Map();
    let lastFilterKeys=[];
    let eventsBound=false;
    let resizeObserver=null;
    let observedMapEl=null;
    let currentMapId='flightMap';
    let currentDataId='flightMapData';

    function getMapElement(mapId){
        return document.getElementById(mapId||'flightMap');
    }

    function ensureMap(mapId){
        const resolvedMapId=mapId||currentMapId||'flightMap';
        const currentEl=getMapElement(resolvedMapId);
        if(!currentEl){
            return false;
        }
        if(!window.L){
            console.warn('[flight-map] Leaflet missing while map element exists');
            return false;
        }

        const requiresRecreate=!map || currentMapId!==resolvedMapId || mapEl!==currentEl;
        if(requiresRecreate && map){
            try{map.remove();}catch(_){ }
            map=null;
            renderer=null;
            layers={past:null,upcoming:null};
            flightIndex.clear();
            lastFilterKeys=[];
        }

        mapEl=currentEl;
        currentMapId=resolvedMapId;

        if(!map){
            map=L.map(mapEl,{center:[20,0],zoom:2,worldCopyJump:true});
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',{attribution:'&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'}).addTo(map);
            renderer=L.canvas({padding:0.2});
            layers={past:L.layerGroup().addTo(map),upcoming:L.layerGroup().addTo(map)};
        }

        return true;
    }

    const cssVar=n=>getComputedStyle(document.documentElement).getPropertyValue(n).trim();
    const colorPast=cssVar('--flight-map-past-line')||'#1565C0';
    const colorUpcoming=cssVar('--flight-map-upcoming-marker')||'#FFB300';
    const colorHighlight=cssVar('--flight-map-highlight')||'#ff4081';

    function decodeHtmlEntities(value){
        if(typeof value!=='string'||value.indexOf('&')===-1){
            return value;
        }
        const textarea=document.createElement('textarea');
        textarea.innerHTML=value;
        return textarea.value;
    }

    function readFlights(dataId){
        const resolvedDataId=dataId||currentDataId||'flightMapData';
        const el=document.getElementById(resolvedDataId);
        if(!el){return [];}
        const raw=(el.textContent||'[]').trim();
        if(!raw){return [];}

        try{
            const parsed=JSON.parse(raw);
            return Array.isArray(parsed)?parsed:[];
        }
        catch(_){
            try{
                const decoded=decodeHtmlEntities(raw);
                const parsed=JSON.parse(decoded);
                return Array.isArray(parsed)?parsed:[];
            }
            catch(e){
                console.warn('[flight-map] JSON parse error',e);
                return [];
            }
        }
    }

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
        if(!map){return;}
        try{ map.invalidateSize(false); }catch(_){ }
        if('requestAnimationFrame' in window){ requestAnimationFrame(()=>{ try{ map.invalidateSize(false);}catch(_){ } }); }
        [16,100,300,800,1500].forEach(ms=>setTimeout(()=>{ try{ map.invalidateSize(false);}catch(_){ } }, ms));
    }
    function ensureGlobalEvents(){
        if(eventsBound){return;}
        eventsBound=true;
        window.addEventListener('resize', scheduleInvalidate);
        window.addEventListener('load', scheduleInvalidate);
        document.addEventListener('visibilitychange',()=>{ if(document.visibilityState==='visible') scheduleInvalidate(); });
        window.addEventListener('flightMap:invalidate', scheduleInvalidate);
    }

    function observeElement(){
        if(!mapEl||!('ResizeObserver' in window)){return;}
        if(!resizeObserver){
            try{ resizeObserver=new ResizeObserver(()=>scheduleInvalidate()); }catch(_){ resizeObserver=null; }
        }
        if(!resizeObserver){return;}
        if(observedMapEl&&observedMapEl!==mapEl){
            try{ resizeObserver.unobserve(observedMapEl); }catch(_){ }
        }
        observedMapEl=mapEl;
        try{ resizeObserver.observe(mapEl); }catch(_){ }
    }

    function render(mapId,dataId){
        currentDataId=dataId||currentDataId||'flightMapData';
        if(!ensureMap(mapId)){return;}
        ensureGlobalEvents();
        observeElement();
        layers.past.clearLayers();layers.upcoming.clearLayers();flightIndex.clear();
        const flights=readFlights(currentDataId);
        if(!flights.length){return;}
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
        if(!ensureMap(currentMapId)){return;}
        if(lastFilterKeys.length){ lastFilterKeys.forEach(k=>{ const e=flightIndex.get(k); if(!e) return; if(e.type==='past'){ e.layer.setStyle(normal.past);} else { e.layer.setStyle(normal.upcoming);} }); }
        if(!term){ lastFilterKeys=[]; return; }
        const termLc=term.toLowerCase();
        lastFilterKeys=Array.from(flightIndex.keys()).filter(k=>k.toLowerCase().includes(termLc));
        lastFilterKeys.forEach(k=>{ const e=flightIndex.get(k); if(!e)return; if(e.type==='past'){ e.layer.setStyle(highlight.past);} else { e.layer.setStyle(highlight.upcoming);} });
    };
    window.flightMapZoomToSelection=function(key){ if(!ensureMap(currentMapId)){return;} const e=flightIndex.get(key); if(!e)return; if(e.type==='past'){ map.fitBounds(e.layer.getBounds().pad(0.3)); } else { const ll=e.layer.getLatLng(); map.setView(ll, Math.max(map.getZoom(),6)); e.layer.openPopup(); } };
    window.flightMapZoomToFiltered=function(){ if(!ensureMap(currentMapId)){return;} if(!lastFilterKeys.length)return; let b=null; lastFilterKeys.forEach(k=>{ const e=flightIndex.get(k); if(!e)return; if(e.type==='past'){ b=b?b.extend(e.layer.getBounds()):e.layer.getBounds(); } else { const ll=e.layer.getLatLng(); b=b?b.extend(ll):L.latLngBounds(ll,ll); } }); if(b) map.fitBounds(b.pad(0.2)); };

    window.flightMapInitOrReload=function(mapId,dataId){
        render(mapId,dataId);
        scheduleInvalidate();
    };

    window.flightMapInitOrReload(currentMapId,currentDataId);
    window.flightMap={reload:render,zoomToSelection:window.flightMapZoomToSelection,zoomToFiltered:window.flightMapZoomToFiltered};
})();
