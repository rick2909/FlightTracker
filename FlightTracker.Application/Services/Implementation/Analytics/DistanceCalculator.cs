using FlightTracker.Application.Services.Interfaces.Analytics;
using FlightTracker.Domain.Entities;
using System;

namespace FlightTracker.Application.Services.Implementation.Analytics;

/// <summary>
/// Haversine-based distance calculator. Stateless & deterministic.
/// </summary>
public class DistanceCalculator : IDistanceCalculator
{
    private const double EarthRadiusKm = 6371.0; // Mean Earth radius

    public double? CalculateGreatCircleKm(Airport from, Airport to)
    {
        if (from.Latitude == null || from.Longitude == null || to.Latitude == null || to.Longitude == null)
            return null;

        double ToRad(double deg) => deg * Math.PI / 180d;

        var lat1 = ToRad(from.Latitude.Value);
        var lon1 = ToRad(from.Longitude.Value);
        var lat2 = ToRad(to.Latitude.Value);
        var lon2 = ToRad(to.Longitude.Value);

        var dLat = lat2 - lat1;
        var dLon = lon2 - lon1;

        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(EarthRadiusKm * c, 1); // 0.1 km precision
    }
}
