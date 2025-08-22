/* Passport JS initializers for chart and map */
(function(w){
  const PRIMARY = '#3f51b5';

  function initPassportChart(flightsPerYear){
    try {
      if (!w.ApexCharts) { return; }
      const el = document.getElementById('passport-chart');
      if (!el) { return; }

      // Normalize input: accept array of {year,count} or object map { "2023": 5, ... }
      let categories = [];
      let values = [];
      if (Array.isArray(flightsPerYear)) {
        categories = flightsPerYear.map(e => (e.year ?? e.Year ?? ''));
        values = flightsPerYear.map(e => (e.count ?? e.Count ?? 0));
      } else if (flightsPerYear && typeof flightsPerYear === 'object') {
        const keys = Object.keys(flightsPerYear).sort((a,b)=>Number(a)-Number(b));
        categories = keys;
        values = keys.map(k => flightsPerYear[k] ?? 0);
      }

      const options = {
        chart: { type: 'area', height: 300, toolbar: { show: false }, animations: { enabled: true } },
        series: [{ name: 'Flights', data: values }],
        xaxis: { categories, labels: { style: { colors: '#424242' } } },
        yaxis: { labels: { style: { colors: '#424242' } } },
        stroke: { curve: 'smooth', width: 2, colors: [PRIMARY] },
        fill: { type: 'gradient', gradient: { shadeIntensity: 0.2, opacityFrom: 0.25, opacityTo: 0.05, stops: [0, 90, 100] } },
        dataLabels: { enabled: false },
        grid: { borderColor: '#E0E0E0' },
        colors: [PRIMARY]
      };

      const chart = new w.ApexCharts(el, options);
      chart.render();
    } catch (_) { /* no-op */ }
  }

  function initPassportMap(routes){
    try {
      if (!w.L) { return; }
      const el = document.getElementById('passport-map');
      if (!el) { return; }

      const map = w.L.map(el, { zoomControl: true, attributionControl: true });
      const tiles = w.L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 18,
        attribution: '&copy; OpenStreetMap contributors'
      });
      tiles.addTo(map);

      const list = Array.isArray(routes) ? routes : [];
      const bounds = w.L.latLngBounds([]);

  list.forEach(r => {
        const fromLat = r.departureLat ?? r.DepartureLat;
        const fromLng = r.departureLon ?? r.DepartureLon;
        const toLat = r.arrivalLat ?? r.ArrivalLat;
        const toLng = r.arrivalLon ?? r.ArrivalLon;
        if (typeof fromLat !== 'number' || typeof fromLng !== 'number' || typeof toLat !== 'number' || typeof toLng !== 'number') {
          return;
        }

        const from = [fromLat, fromLng];
        const to = [toLat, toLng];

        const line = w.L.polyline([from, to], { color: PRIMARY, weight: 2, opacity: 0.8 });
        line.addTo(map);

        const start = w.L.circleMarker(from, { radius: 3, color: PRIMARY, weight: 1, fillColor: PRIMARY, fillOpacity: 0.9 });
        const end = w.L.circleMarker(to, { radius: 3, color: PRIMARY, weight: 1, fillColor: PRIMARY, fillOpacity: 0.9 });
        start.addTo(map);
        end.addTo(map);

        bounds.extend(from);
        bounds.extend(to);
      });

      if (bounds.isValid()) {
        map.fitBounds(bounds.pad(0.2));
      } else {
        map.setView([20, 0], 2);
      }
    } catch (_) { /* no-op */ }
  }

  w.Passport = { initPassportChart, initPassportMap };
})(window);
