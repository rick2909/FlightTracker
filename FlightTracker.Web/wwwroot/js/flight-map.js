// Leaflet flight map initialization with mock data; ready for future API integration
(function() {
    const mapEl = document.getElementById('flightMap');
    if (!mapEl || !window.L) { return; }

    const cssVar = name => getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    const colorPast = cssVar('--flight-map-past-line') || '#1565C0';
    const colorUpcoming = cssVar('--flight-map-upcoming-marker') || '#FFB300';

    // Mock data (replace with API calls later)
    const pastFlights = [
        { id: 'FT1001', path: [ [52.3086, 4.7639], [51.4700, -0.4543], [40.6413, -73.7781] ] }, // AMS -> LHR -> JFK
        { id: 'FT1002', path: [ [48.3538, 11.7861], [41.9786, -87.9048], [33.9416, -118.4085] ] }, // MUC -> ORD -> LAX
        { id: 'FT1003', path: [ [35.5494, 139.7798], [37.6213, -122.3790] ] } // HND -> SFO
    ];

    const upcomingFlights = [
        { id: 'FT2001', number: 'LH123', departure: '2025-08-14T09:30:00Z', coord: [50.0379, 8.5622] }, // FRA
        { id: 'FT2002', number: 'BA456', departure: '2025-08-14T11:15:00Z', coord: [51.4700, -0.4543] }, // LHR
        { id: 'FT2003', number: 'DL789', departure: '2025-08-14T13:00:00Z', coord: [33.6407, -84.4277] } // ATL
    ];

    const map = L.map('flightMap', {
        center: [50, 10], // Europe
        zoom: 4,
        worldCopyJump: true
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    const layerGroups = {
        past: L.layerGroup().addTo(map),
        upcoming: L.layerGroup().addTo(map)
    };

    const polylineList = [];
    pastFlights.forEach(f => {
        const line = L.polyline(f.path, {
            color: colorPast,
            weight: 3,
            opacity: 0.75
        }).addTo(layerGroups.past);
        polylineList.push(line);
    });

    const markerList = [];
    upcomingFlights.forEach(f => {
        const marker = L.circleMarker(f.coord, {
            radius: 7,
            color: colorUpcoming,
            fillColor: colorUpcoming,
            fillOpacity: 0.9,
            weight: 2
        }).addTo(layerGroups.upcoming);
        marker.bindPopup(`<strong>${f.number}</strong><br/>Departs: ${new Date(f.departure).toUTCString()}`);
        markerList.push(marker);
    });

    // Fit bounds to all geometry
    let bounds = null;
    polylineList.forEach(pl => {
        bounds = bounds ? bounds.extend(pl.getBounds()) : pl.getBounds();
    });
    markerList.forEach(m => {
        const ll = m.getLatLng();
        bounds = bounds ? bounds.extend(ll) : L.latLngBounds(ll, ll);
    });
    if (bounds) {
        map.fitBounds(bounds.pad(0.15));
    }

    // Lightweight search filter (client-side mock)
    const searchInput = document.getElementById('flightSearch');
    if (searchInput) {
        searchInput.addEventListener('input', e => {
            const q = e.target.value.toLowerCase();
            markerList.forEach(m => {
                const content = (m.getPopup()?.getContent() || '').toLowerCase();
                const match = content.includes(q);
                m.setStyle({ opacity: match || !q ? 1 : 0.25, fillOpacity: match || !q ? 0.9 : 0.25 });
            });
        });
    }

    // Public hook for future dynamic refresh
    window.flightMap = {
        refresh: function({ past = [], upcoming = [] } = {}) {
            layerGroups.past.clearLayers();
            layerGroups.upcoming.clearLayers();
            // TODO: implement dynamic update using provided arrays
        }
    };
})();
