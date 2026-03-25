// Airports overview map
(function(){
  let map = null;
  let mapEl = null;
  let markersLayer = null;
  let selectedAirport = null;
  let selectedMarker = null;
  let markerIndex = new Map();
  let lastReq = 0;
  let useLive = false;
  let dotNetRef = null;

  function ensureMap(){
    if(!window.L){ try{ console.error('[airports-map] Leaflet missing'); }catch{} return false; }

    const currentEl = document.getElementById('airportsMap');
    if(!currentEl){ return false; }

    const requiresRecreate = !map || mapEl !== currentEl;
    if(requiresRecreate && map){
      try{ map.remove(); }catch(_){ }
      map = null;
      markersLayer = null;
      selectedAirport = null;
      selectedMarker = null;
      markerIndex = new Map();
    }

    mapEl = currentEl;

    if(!map){
      map = L.map(mapEl, { center:[20,0], zoom:4, worldCopyJump:true });
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
      }).addTo(map);

      markersLayer = L.layerGroup().addTo(map);
      map.on('moveend zoomend', () => loadAirports());
    }

    return true;
  }

  function boundsQuery(){
    if(!map){ return ''; }
    const b = map.getBounds();
    const z = map.getZoom();
    return `north=${b.getNorth()}&south=${b.getSouth()}&east=${b.getEast()}&west=${b.getWest()}&zoom=${z}`;
  }

  async function loadAirports(){
    if(!ensureMap()){ return; }
    if(!dotNetRef){ return; }
    const reqId = ++lastReq;
    const b = map.getBounds();
    const z = map.getZoom();
    try{
      const airports = await dotNetRef.invokeMethodAsync(
        'GetAirportsForMapAsync',
        b.getNorth(),
        b.getSouth(),
        b.getEast(),
        b.getWest(),
        z
      );
      const list = (airports || []).map(a => ({
        id: a.id,
        name: a.name,
        city: a.city,
        country: a.country,
        iata: a.iata,
        icao: a.icao,
        lat: a.lat,
        lon: a.lon
      }));
      if(reqId !== lastReq) return; // stale
      markersLayer.clearLayers();
      markerIndex = new Map();
      list.forEach(a => {
        if(a.lat == null || a.lon == null) return;
        const label = `${a.name}${a.iata?` (${a.iata})`: (a.icao?` (${a.icao})`: '')}`;
        const color = getCssVar('--airports-marker') || '#3f51b5';
        const outline = getCssVar('--airports-marker-outline') || color;
        const m = L.circleMarker([a.lat, a.lon], { radius:6, color:outline, weight:2, fill:true, fillOpacity:0.85, fillColor: color });
        m.bindTooltip(label);
        m.on('click', () => selectAirport(a));
        m.addTo(markersLayer);
        markerIndex.set(a.id, m);
      });

      // Keep selected airport visible and highlighted across zoom/bounds
      if(selectedAirport && selectedAirport.lat != null && selectedAirport.lon != null){
        let sm = markerIndex.get(selectedAirport.id);
        if(!sm){
          const label = `${selectedAirport.name}${selectedAirport.iata?` (${selectedAirport.iata})`: (selectedAirport.icao?` (${selectedAirport.icao})`: '')}`;
          const base = getCssVar('--airports-marker') || '#3f51b5';
          const outline = getCssVar('--airports-marker-outline') || base;
          sm = L.circleMarker([selectedAirport.lat, selectedAirport.lon], { radius:6, color:outline, weight:2, fill:true, fillOpacity:0.85, fillColor: base });
          sm.bindTooltip(label);
          sm.on('click', () => selectAirport(selectedAirport));
          sm.addTo(markersLayer);
          markerIndex.set(selectedAirport.id, sm);
        }
        if(selectedMarker && selectedMarker !== sm){
          setMarkerSelected(selectedMarker, false);
        }
        setMarkerSelected(sm, true);
        selectedMarker = sm;
      }
  }catch(e){ try{ console.error('[airports-map] load error', e); }catch{} }
  }

  function getCssVar(name){
    return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  }

  function setMarkerSelected(marker, isSelected){
    const base = getCssVar('--airports-marker') || '#3f51b5';
    const selected = getCssVar('--airports-marker-selected') || '#29B6F6';
    const outline = getCssVar('--airports-marker-outline') || base;
    const color = isSelected ? selected : base;
    marker.setStyle({
      color: outline,
      fillColor: color,
      weight: isSelected ? 3 : 2,
      radius: isSelected ? 8 : 6,
      fillOpacity: isSelected ? 0.95 : 0.85
    });
  }

  async function selectAirport(a){
    if(!a){ return; }
    selectedAirport = a;
    if(selectedMarker){ setMarkerSelected(selectedMarker, false); }
    const m = markerIndex.get(a.id);
    if(m){ setMarkerSelected(m, true); selectedMarker = m; }
    document.getElementById('airportSelection').hidden = false;
    document.getElementById('selectedAirportName').textContent = `${a.name}${a.iata?` (${a.iata})`: (a.icao?` (${a.icao})`: '')}`;
    await loadFlights(a);
  }

  function renderFlightItem(f){
    const div = document.createElement('div');
    div.className = 'airport-flight-item rz-p-2';
    const left = document.createElement('div');
    left.className = 'airport-flight-item__left';
    left.innerHTML = `<div class="airport-flight-item__title">${f.flightNumber} • ${f.route}</div>
                      <div class="airport-flight-item__subtitle">${f.airline ?? '—'}${f.aircraft? ` • ${f.aircraft}`:''}</div>`;
    const right = document.createElement('div');
    const btn = document.createElement('button');
    btn.className = 'rz-button rz-primary rz-sm';
    btn.type = 'button';
    btn.title = 'Add to my flights';
    btn.textContent = 'Add flight';
    // Wire to open Blazor dialog via interop
    btn.addEventListener('click', () => {
      const data = {
        id: f.id,
        flightNumber: f.flightNumber,
        route: f.route,
        departureCode: f.departureCode ?? null,
        arrivalCode: f.arrivalCode ?? null,
        departureTimeUtc: f.departureTimeUtc,
        arrivalTimeUtc: f.arrivalTimeUtc
      };
      if(window.FT && typeof window.FT.openAddFlight === 'function'){
        window.FT.openAddFlight(data);
      } else {
        try{ console.error('[airports-map] FT.openAddFlight not available'); }catch{}
      }
    });
    right.appendChild(btn);
    div.appendChild(left);
    div.appendChild(right);
    return div;
  }

  async function loadFlights(airport){
    if(!airport){ return; }
    if(!dotNetRef){ return; }
    try{
      const data = await dotNetRef.invokeMethodAsync(
        'GetFlightsForAirportAsync',
        airport.id,
        useLive
      );
      const depEl = document.getElementById('departingList');
      const arrEl = document.getElementById('arrivingList');
      depEl.innerHTML = '';
      arrEl.innerHTML = '';
      (data.departing ?? []).forEach(f => depEl.appendChild(renderFlightItem(f)));
      (data.arriving ?? []).forEach(f => arrEl.appendChild(renderFlightItem(f)));
  }catch(e){ try{ console.error('[airports-map] flights error', e); }catch{} }
  }

  const toggle = document.getElementById('airportsToggleFlights');
  if(toggle){
    useLive = !!toggle.checked;
    toggle.addEventListener('change', () => {
      useLive = !!toggle.checked;
      if(selectedAirport){ loadFlights(selectedAirport); }
    });
  }

  function initOrReload(dotNet){
    dotNetRef = dotNet || dotNetRef;
    if(!ensureMap()){ return; }
    loadAirports();
    try{ map.invalidateSize(false); }catch(_){ }
    if('requestAnimationFrame' in window){
      requestAnimationFrame(() => { try{ map.invalidateSize(false); }catch(_){ } });
    }
  }

  window.airportsMapInitOrReload = initOrReload;
  initOrReload(null);
})();