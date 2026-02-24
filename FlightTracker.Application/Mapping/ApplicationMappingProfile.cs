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
        CreateMap<AirportEnrichmentDto, Airport>()
            .ForMember(dest => dest.City,
                opt => opt.MapFrom(src => src.Municipality ?? string.Empty))
            .ForMember(dest => dest.Country,
                opt => opt.MapFrom(src => src.CountryName ?? string.Empty))
            .ForMember(dest => dest.Name,
                opt => opt.MapFrom(src => src.Name ?? string.Empty))
            .ForMember(dest => dest.TimeZoneId,
                opt => opt.MapFrom(src => (string?)null));
        CreateMap<Aircraft, AircraftDto>()
            .ForMember(dest => dest.AirlineIcaoCode,
                opt => opt.MapFrom(src => src.Airline != null ? src.Airline.IcaoCode : null))
            .ForMember(dest => dest.AirlineIataCode,
                opt => opt.MapFrom(src => src.Airline != null ? src.Airline.IataCode : null))
            .ForMember(dest => dest.AirlineName,
                opt => opt.MapFrom(src => src.Airline != null ? src.Airline.Name : null));

        CreateMap<CreateAircraftDto, Aircraft>();

        // UserPreferences mapping
        CreateMap<UserPreferences, UserPreferencesDto>();
        CreateMap<UserPreferencesDto, UserPreferences>();
    }
}
