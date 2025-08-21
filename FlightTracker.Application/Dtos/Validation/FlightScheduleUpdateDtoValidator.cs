using FluentValidation;

namespace FlightTracker.Application.Dtos.Validation;

public class FlightScheduleUpdateDtoValidator : AbstractValidator<FlightScheduleUpdateDto>
{
    public FlightScheduleUpdateDtoValidator()
    {
        RuleFor(x => x.FlightId)
            .GreaterThan(0);

        RuleFor(x => x.FlightNumber)
            .NotEmpty()
            .MaximumLength(12);

        RuleFor(x => x.DepartureAirportCode)
            .NotEmpty();

        RuleFor(x => x.ArrivalAirportCode)
            .NotEmpty();

        RuleFor(x => x.ArrivalTimeUtc)
            .Must((x, arrivalTimeUtc) => arrivalTimeUtc > x.DepartureTimeUtc)
            .WithMessage("Arrival time must be after departure time.");
    }
}
