using FluentValidation;

namespace FlightTracker.Application.Dtos.Validation;

public class CreateUserFlightDtoValidator : AbstractValidator<CreateUserFlightDto>
{
    public CreateUserFlightDtoValidator()
    {
        RuleFor(x => x.FlightId)
            .GreaterThanOrEqualTo(0);

        When(x => x.FlightId == 0, () =>
        {
            RuleFor(x => x.FlightNumber)
                .NotEmpty()
                .MaximumLength(12);

            RuleFor(x => x.DepartureAirportCode)
                .NotEmpty();

            RuleFor(x => x.ArrivalAirportCode)
                .NotEmpty();

            RuleFor(x => x.DepartureTimeUtc)
                .NotNull();

            RuleFor(x => x.ArrivalTimeUtc)
                .NotNull();

            RuleFor(x => x)
                .Must(ValidateArrivalAfterDeparture)
                .WithMessage("Arrival time must be after departure time.");
        });

        RuleFor(x => x.SeatNumber)
            .MaximumLength(10);

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .When(x => x.Notes != null);
    }

    private bool ValidateArrivalAfterDeparture(CreateUserFlightDto dto)
    {
        return dto.DepartureTimeUtc.HasValue
            && dto.ArrivalTimeUtc.HasValue
            && dto.ArrivalTimeUtc.Value > dto.DepartureTimeUtc.Value;
    }
}
