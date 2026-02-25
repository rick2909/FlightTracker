using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Web.Models;
using FlightTracker.Web.Models.ViewModels;

namespace FlightTracker.Web.Mapping;

/// <summary>
/// AutoMapper profile for Web layer ViewModels.
/// </summary>
public class WebMappingProfile : Profile
{
    public WebMappingProfile()
    {
        CreateMap<UserFlightDto, EditUserFlightViewModel>()
            .ForMember(dest => dest.UserFlightId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.DepartureAirportCode,
                opt => opt.MapFrom(src => src.DepartureIataCode ?? src.DepartureIcaoCode ?? src.DepartureAirportCode))
            .ForMember(dest => dest.ArrivalAirportCode,
                opt => opt.MapFrom(src => src.ArrivalIataCode ?? src.ArrivalIcaoCode ?? src.ArrivalAirportCode))
            .ForMember(dest => dest.AircraftRegistration,
                opt => opt.MapFrom(src => src.Aircraft != null ? src.Aircraft.Registration : null))
            .ForMember(dest => dest.OperatingAirlineCode,
                opt => opt.MapFrom(src => src.OperatingAirlineIataCode ?? src.OperatingAirlineIcaoCode));

        CreateMap<EditUserFlightViewModel, UpdateUserFlightDto>();

        CreateMap<EditUserFlightViewModel, FlightScheduleUpdateDto>()
            .ForMember(dest => dest.DepartureAirportCode,
                opt => opt.MapFrom(src => src.DepartureAirportCode ?? string.Empty))
            .ForMember(dest => dest.ArrivalAirportCode,
                opt => opt.MapFrom(src => src.ArrivalAirportCode ?? string.Empty));

        CreateMap<PreferencesViewModel, UserPreferencesDto>()
            .ForMember(dest => dest.ProfileVisibility,
                opt => opt.MapFrom(src => src.ProfileVisibilityLevel));
    }
}
