using System;
using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;

namespace FlightTracker.Application.Mapping;

/// <summary>
/// AutoMapper profile for Application layer DTOs.
/// </summary>
public class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        CreateMap<Airport, AirportDto>();
        CreateMap<Aircraft, AircraftDto>();
    }
}
