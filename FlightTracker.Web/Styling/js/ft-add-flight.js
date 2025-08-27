// FT Add Flight interop: allows plain JS to open the Blazor AddFlight dialog
(function () {
  window.FT = window.FT || {};
  let _dotnet = null;

  // Called by Blazor component on first render
  window.FT.registerAddFlight = function (dotnetRef) {
    _dotnet = dotnetRef;
  };

  // Called by plain JS (e.g., airports-map) to open dialog with data
  window.FT.openAddFlight = function (data) {
    if (!_dotnet || !_dotnet.invokeMethodAsync) {
      try { console.error('[ft-add-flight] launcher not ready'); } catch {}
      return;
    }
    try {
      _dotnet.invokeMethodAsync('OpenWithData', sanitize(data));
    } catch (e) {
      try { console.error('[ft-add-flight] invoke failed', e); } catch {}
    }
  };

  function sanitize(d) {
    if (!d || typeof d !== 'object') return {};
    const out = {
      id: toInt(d.id),
      flightNumber: toStr(d.flightNumber),
      departureCode: toStr(d.departureCode),
      arrivalCode: toStr(d.arrivalCode),
      departureTimeUtc: toStr(d.departureTimeUtc),
      arrivalTimeUtc: toStr(d.arrivalTimeUtc),
      route: toStr(d.route)
    };
    // Fallback: derive codes from route like "AMS → JFK"
    if (!out.departureCode && !out.arrivalCode && out.route) {
      const parts = out.route.split('→');
      if (parts.length === 2) {
        out.departureCode = parts[0].trim();
        out.arrivalCode = parts[1].trim();
      }
    }
    return out;
  }

  function toStr(v) { return (v == null) ? null : String(v); }
  function toInt(v) { const n = Number.parseInt(v, 10); return Number.isFinite(n) ? n : 0; }
})();
