using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Mapping;

/// <summary>
/// AutoMapper profile for Application layer DTOs.
/// </summary>
public class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        CreateMap<Flight, FlightDto>();
        CreateMap<Flight, FlightDetailDto>();
        CreateMap<Airport, AirportDto>();
        CreateMap<Aircraft, AircraftDto>();
    }
}
