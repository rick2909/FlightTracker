using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using FlightTracker.Web.Services.Interfaces;
using FlightTracker.Domain.Entities;
using FlightTracker.Application.Dtos;

namespace FlightTracker.Web.Pages;

[Authorize]
public partial class Airports : IDisposable
{
	[Inject]
	private IJSRuntime JsRuntime { get; set; } = default!;

	[Inject]
	private IAirportsApiClient AirportsApiClient { get; set; } = default!;

	private DotNetObjectReference<Airports>? _dotNetRef;

	[JSInvokable]
	public async Task<IReadOnlyList<AirportMapItem>> GetAirportsForMapAsync(
		double north,
		double south,
		double east,
		double west,
		int zoom)
	{
		if (zoom <= 3)
		{
			return Array.Empty<AirportMapItem>();
		}

		var airports = await AirportsApiClient.GetAirportsAsync();

		bool crossesAntimeridian = east < west;
		var inView = airports
			.Where(a => a.Latitude.HasValue && a.Longitude.HasValue)
			.Where(a =>
			{
				var lat = a.Latitude!.Value;
				var lon = a.Longitude!.Value;
				var withinLat = lat <= north && lat >= south;
				var withinLon = crossesAntimeridian
					? (lon >= west || lon <= east)
					: (lon >= west && lon <= east);
				return withinLat && withinLon;
			});

		if (zoom is >= 4 and < 6)
		{
			inView = inView.Where(a => !string.IsNullOrWhiteSpace(a.IataCode));
		}
		else if (zoom is >= 6 and < 8)
		{
			inView = inView.Where(
				a => !string.IsNullOrWhiteSpace(a.IataCode)
				  || !string.IsNullOrWhiteSpace(a.City));
		}

		return inView
			.Select(MapAirport)
			.ToList();
	}

	[JSInvokable]
	public async Task<AirportFlightsMapPayload> GetFlightsForAirportAsync(
		int airportId,
		bool live)
	{
		var airports = await AirportsApiClient.GetAirportsAsync();
		var airport = airports.FirstOrDefault(a => a.Id == airportId);

		if (airport is null)
		{
			return new AirportFlightsMapPayload();
		}

		var code = airport.IataCode ?? airport.IcaoCode ?? airport.Name;
		var flights = await AirportsApiClient.GetFlightsAsync(
			code,
			null,
			live,
			100);

		return new AirportFlightsMapPayload
		{
			Departing = flights.Departing.Select(MapFlight).ToList(),
			Arriving = flights.Arriving.Select(MapFlight).ToList()
		};
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		_dotNetRef ??= DotNetObjectReference.Create(this);

		try
		{
			await JsRuntime.InvokeVoidAsync("airportsMapInitOrReload", _dotNetRef);
		}
		catch (JSDisconnectedException)
		{
			// Ignore transient disconnects during navigation/teardown.
		}
		catch (TaskCanceledException)
		{
			// Ignore canceled invocations during rapid route changes.
		}
	}

	public void Dispose()
	{
		_dotNetRef?.Dispose();
		_dotNetRef = null;
	}

	private static AirportMapItem MapAirport(Airport airport)
	{
		return new AirportMapItem
		{
			Id = airport.Id,
			Name = airport.Name,
			City = airport.City,
			Country = airport.Country,
			Iata = airport.IataCode,
			Icao = airport.IcaoCode,
			Lat = airport.Latitude,
			Lon = airport.Longitude
		};
	}

	private static AirportFlightMapItem MapFlight(AirportFlightListItemDto f)
	{
		return new AirportFlightMapItem
		{
			Id = f.Id,
			FlightNumber = f.FlightNumber,
			Airline = f.Airline,
			Aircraft = f.Aircraft,
			Route = $"{f.DepartureCode} -> {f.ArrivalCode}",
			DepartureCode = f.DepartureCode,
			ArrivalCode = f.ArrivalCode,
			DepartureTimeUtc = f.DepartureTimeUtc,
			ArrivalTimeUtc = f.ArrivalTimeUtc
		};
	}

	public sealed class AirportMapItem
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? City { get; set; }
		public string? Country { get; set; }
		public string? Iata { get; set; }
		public string? Icao { get; set; }
		public double? Lat { get; set; }
		public double? Lon { get; set; }
	}

	public sealed class AirportFlightMapItem
	{
		public int? Id { get; set; }
		public string FlightNumber { get; set; } = string.Empty;
		public string? Airline { get; set; }
		public string? Aircraft { get; set; }
		public string? Route { get; set; }
		public string? DepartureCode { get; set; }
		public string? ArrivalCode { get; set; }
		public string? DepartureTimeUtc { get; set; }
		public string? ArrivalTimeUtc { get; set; }
	}

	public sealed class AirportFlightsMapPayload
	{
		public IReadOnlyList<AirportFlightMapItem> Departing { get; set; }
			= Array.Empty<AirportFlightMapItem>();

		public IReadOnlyList<AirportFlightMapItem> Arriving { get; set; }
			= Array.Empty<AirportFlightMapItem>();
	}
}
