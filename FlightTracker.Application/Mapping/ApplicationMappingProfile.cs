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
        CreateMap<Aircraft, AircraftDto>()
            .ForMember(dest => dest.AirlineIcaoCode,
                opt => opt.MapFrom(src => src.Airline != null ? src.Airline.IcaoCode : null))
            .ForMember(dest => dest.AirlineIataCode,
                opt => opt.MapFrom(src => src.Airline != null ? src.Airline.IataCode : null))
            .ForMember(dest => dest.AirlineName,
                opt => opt.MapFrom(src => src.Airline != null ? src.Airline.Name : null));

        CreateMap<CreateAircraftDto, Aircraft>();
    }
}
