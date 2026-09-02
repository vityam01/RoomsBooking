using ConferenceRoomApi.Application.Rooms.Dtos;
using ConferenceRoomApi.Domain.Pricing;
using FluentValidation;

namespace ConferenceRoomApi.Application.Rooms.Validators;

public sealed class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Capacity).GreaterThan(0).LessThanOrEqualTo(10_000);
        RuleFor(x => x.BasePricePerHour).GreaterThan(0);
        RuleForEach(x => x.AdditionalServiceIds).NotEmpty();
    }
}

public sealed class UpdateRoomRequestValidator : AbstractValidator<UpdateRoomRequest>
{
    public UpdateRoomRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Capacity).GreaterThan(0).LessThanOrEqualTo(10_000);
        RuleFor(x => x.BasePricePerHour).GreaterThan(0);
        RuleForEach(x => x.AdditionalServiceIds).NotEmpty();
    }
}

public sealed class SearchAvailableRoomsRequestValidator : AbstractValidator<SearchAvailableRoomsRequest>
{
    public SearchAvailableRoomsRequestValidator()
    {
        RuleFor(x => x.Capacity).GreaterThan(0);
        RuleFor(x => x.StartTime)
            .LessThan(x => x.EndTime)
            .WithMessage("StartTime must be before EndTime.");
        RuleFor(x => x.StartTime).GreaterThanOrEqualTo(BusinessHours.OpensAt);
        RuleFor(x => x.EndTime).LessThanOrEqualTo(BusinessHours.ClosesAt);
    }
}
