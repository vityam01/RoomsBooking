using ConferenceRoomApi.Application.Bookings.Dtos;
using FluentValidation;

namespace ConferenceRoomApi.Application.Bookings.Validators;

public sealed class BookingListFilterValidator : AbstractValidator<BookingListFilter>
{
    public BookingListFilterValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.From)
            .LessThanOrEqualTo(x => x.To!.Value)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("From must not be after To.");
    }
}
