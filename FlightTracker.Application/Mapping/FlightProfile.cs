using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;

namespace FlightTracker.Application.Mapping;

public class FlightProfile : Profile
{
    public FlightProfile()
    {
        CreateMap<Flight, FlightDto>();
        CreateMap<Flight, FlightDetailDto>();

        CreateMap<UserFlight, UserFlightDto>()
            .ForMember(dest => dest.FlightNumber,
                opt => opt.MapFrom(src => src.Flight != null ? src.Flight.FlightNumber : string.Empty))
            .ForMember(dest => dest.FlightStatus,
                opt => opt.MapFrom(src => src.Flight != null ? src.Flight.Status : FlightStatus.Scheduled))
            .ForMember(dest => dest.DepartureTimeUtc,
                opt => opt.MapFrom(src => src.Flight != null ? src.Flight.DepartureTimeUtc : DateTime.MinValue))
            .ForMember(dest => dest.ArrivalTimeUtc,
                opt => opt.MapFrom(src => src.Flight != null ? src.Flight.ArrivalTimeUtc : DateTime.MinValue))
            .ForMember(dest => dest.OperatingAirlineId,
                opt => opt.MapFrom(src => src.Flight != null ? src.Flight.OperatingAirlineId : null))
            .ForMember(dest => dest.OperatingAirlineIcaoCode,
                opt => opt.MapFrom(src => src.Flight != null && src.Flight.OperatingAirline != null
                    ? src.Flight.OperatingAirline.IcaoCode
                    : null))
            .ForMember(dest => dest.OperatingAirlineIataCode,
                opt => opt.MapFrom(src => src.Flight != null && src.Flight.OperatingAirline != null
                    ? src.Flight.OperatingAirline.IataCode
                    : null))
            .ForMember(dest => dest.OperatingAirlineName,
                opt => opt.MapFrom(src => src.Flight != null && src.Flight.OperatingAirline != null
                    ? src.Flight.OperatingAirline.Name
                    : null))
            .ForMember(dest => dest.DepartureAirportCode,
                opt => opt.MapFrom(src => src.Flight != null && src.Flight.DepartureAirport != null
                    ? (src.Flight.DepartureAirport.IataCode ?? src.Flight.DepartureAirport.IcaoCode ?? string.Empty)
                    : string.Empty))
            .ForMember(dest => dest.DepartureIataCode,
                opt => opt.MapFrom(src => src.Flight != null && src.Flight.DepartureAirport != null
                    ? src.Flight.DepartureAirport.IataCode
                    : null))
            .ForMember(dest => dest.DepartureIcaoCode,
                opt => opt.MapFrom(src => src.Flight != null && src.Flight.DepartureAirport != null
                    ? src.Flight.DepartureAirport.IcaoCode
                    : null))
            .ForMember(dest => dest.DepartureAirportName,
                opt => opt.MapFrom(src => src.Flight != null && src.Flight.DepartureAirport != null
                    ? src.Flight.DepartureAirport.Name ?? string.Empty
                    : string.Empty))
            .ForMember(dest => dest.DepartureCity,
                opt => opt.MapFrom(src => src.Flight != null && src.Flight.DepartureAirport != null
                    ? src.Flight.DepartureAirport.City ?? string.Empty
                    : string.Empty))
            .ForMember(dest => dest.ArrivalAirportCode,
                opt => opt.MapFrom(src => src.Flight != null && src.Flight.ArrivalAirport != null
                    ? (src.Flight.ArrivalAirport.IataCode ?? src.Flight.ArrivalAirport.IcaoCode ?? string.Empty)
                    : string.Empty))
            .ForMember(dest => dest.ArrivalIataCode,
                opt => opt.MapFrom(src => src.Flight != null && src.Flight.ArrivalAirport != null
                    ? src.Flight.ArrivalAirport.IataCode
                    : null))
            .ForMember(dest => dest.ArrivalIcaoCode,
                opt => opt.MapFrom(src => src.Flight != null && src.Flight.ArrivalAirport != null
                    ? src.Flight.ArrivalAirport.IcaoCode
                    : null))
            .ForMember(dest => dest.ArrivalAirportName,
                opt => opt.MapFrom(src => src.Flight != null && src.Flight.ArrivalAirport != null
                    ? src.Flight.ArrivalAirport.Name ?? string.Empty
                    : string.Empty))
            .ForMember(dest => dest.ArrivalCity,
                opt => opt.MapFrom(src => src.Flight != null && src.Flight.ArrivalAirport != null
                    ? src.Flight.ArrivalAirport.City ?? string.Empty
                    : string.Empty))
            .ForMember(dest => dest.Aircraft,
                opt => opt.MapFrom(src => src.Flight != null ? src.Flight.Aircraft : null))
            .ForMember(dest => dest.DepartureTimeZoneId, opt => opt.Ignore())
            .ForMember(dest => dest.ArrivalTimeZoneId, opt => opt.Ignore());
    }
}