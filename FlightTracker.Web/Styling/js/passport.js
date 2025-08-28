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

  function initPie(targetId, dict, label){
    try {
      if (!w.ApexCharts) { return; }
      const el = document.getElementById(targetId);
      if (!el) { return; }

      const keys = Object.keys(dict || {});
      const series = keys.map(k => dict[k] ?? 0);
      const options = {
        chart: { type: 'donut', height: 260, toolbar: { show: false } },
        series,
        labels: keys,
        legend: { position: 'bottom' },
        dataLabels: { enabled: true },
        tooltip: { y: { formatter: (val) => `${val}` } },
        noData: { text: 'No data' },
        title: label ? { text: '', align: 'center' } : undefined
      };
      const chart = new w.ApexCharts(el, options);
      chart.render();
    } catch (_) { /* no-op */ }
  }


  // Only chart initializer here. Map rendering handled by shared flight-map.js
  w.Passport = { initPassportChart, initPie };
})(window);
