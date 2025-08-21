using FluentValidation;

namespace FlightTracker.Application.Dtos.Validation;

public class UpdateUserFlightDtoValidator : AbstractValidator<UpdateUserFlightDto>
{
    public UpdateUserFlightDtoValidator()
    {
        RuleFor(x => x.SeatNumber)
            .MaximumLength(10);

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .When(x => x.Notes != null);
    }
}
