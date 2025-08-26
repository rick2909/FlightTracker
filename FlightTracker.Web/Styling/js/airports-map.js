// Airports overview map
(function(){
  if(!window.L){ console.warn('[airports-map] Leaflet missing'); return; }

  const el = document.getElementById('airportsMap');
  if(!el){ return; }

  const map = L.map('airportsMap', { center:[20,0], zoom:2, worldCopyJump:true });
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; OpenStreetMap contributors'
  }).addTo(map);

  const markersLayer = L.layerGroup().addTo(map);
  let lastReq = 0;

  function boundsQuery(){
    const b = map.getBounds();
    const z = map.getZoom();
    return `north=${b.getNorth()}&south=${b.getSouth()}&east=${b.getEast()}&west=${b.getWest()}&zoom=${z}`;
  }

  async function loadAirports(){
    const reqId = ++lastReq;
    const url = `/Airports/Browse?${boundsQuery()}`;
    try{
      const res = await fetch(url, { headers:{ 'Accept':'application/json' } });
      if(!res.ok) throw new Error('HTTP '+res.status);
      const list = await res.json();
      if(reqId !== lastReq) return; // stale
      markersLayer.clearLayers();
      list.forEach(a => {
        if(a.lat == null || a.lon == null) return;
        const label = `${a.name}${a.iata?` (${a.iata})`: (a.icao?` (${a.icao})`: '')}`;
        const m = L.circleMarker([a.lat, a.lon], { radius:6, color:'#1976d2', weight:2, fill:true, fillOpacity:0.7 });
        m.bindTooltip(label);
        m.on('click', () => selectAirport(a));
        m.addTo(markersLayer);
      });
    }catch(e){ console.warn('[airports-map] load error', e); }
  }

  let selectedAirport = null;

  async function selectAirport(a){
    selectedAirport = a;
    document.getElementById('airportSelection').hidden = false;
    document.getElementById('selectedAirportName').textContent = `${a.name}${a.iata?` (${a.iata})`: (a.icao?` (${a.icao})`: '')}`;
    await loadFlights(a.id);
  }

  function renderFlightItem(f){
    const div = document.createElement('div');
    div.className = 'list-group-item d-flex justify-content-between align-items-center';
    const left = document.createElement('div');
    left.innerHTML = `<div class="fw-semibold">${f.flightNumber} • ${f.route}</div>
                      <div class="text-muted small">${f.airline ?? '—'}${f.aircraft? ` • ${f.aircraft}`:''}</div>`;
    const right = document.createElement('div');
    const btn = document.createElement('button');
    btn.className = 'btn btn-sm btn-primary';
    btn.type = 'button';
    btn.title = 'Add to my flights';
    btn.textContent = 'Add flight';
    // Hook for later wiring
    btn.addEventListener('click', () => {
      console.log('Add flight clicked for', f);
    });
    right.appendChild(btn);
    div.appendChild(left);
    div.appendChild(right);
    return div;
  }

  async function loadFlights(airportId){
    const url = `/Airports/${airportId}/Flights`;
    try{
      const res = await fetch(url, { headers:{ 'Accept':'application/json' } });
      if(!res.ok) throw new Error('HTTP '+res.status);
      const data = await res.json();
      const depEl = document.getElementById('departingList');
      const arrEl = document.getElementById('arrivingList');
      depEl.innerHTML = '';
      arrEl.innerHTML = '';
      (data.departing ?? []).forEach(f => depEl.appendChild(renderFlightItem(f)));
      (data.arriving ?? []).forEach(f => arrEl.appendChild(renderFlightItem(f)));
    }catch(e){ console.warn('[airports-map] flights error', e); }
  }

  map.on('moveend zoomend', () => loadAirports());
  loadAirports();
})();
