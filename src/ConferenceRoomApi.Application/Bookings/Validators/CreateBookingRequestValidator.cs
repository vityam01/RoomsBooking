using ConferenceRoomApi.Application.Bookings.Dtos;
using ConferenceRoomApi.Domain.Pricing;
using FluentValidation;

namespace ConferenceRoomApi.Application.Bookings.Validators;

public sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.StartTime)
            .LessThan(x => x.EndTime)
            .WithMessage("StartTime must be before EndTime.");
        RuleFor(x => x.StartTime).GreaterThanOrEqualTo(BusinessHours.OpensAt);
        RuleFor(x => x.EndTime).LessThanOrEqualTo(BusinessHours.ClosesAt);
        RuleForEach(x => x.AdditionalServiceIds).NotEmpty();
    }
}
