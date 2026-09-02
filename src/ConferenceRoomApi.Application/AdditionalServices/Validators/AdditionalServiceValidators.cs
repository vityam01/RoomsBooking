using ConferenceRoomApi.Application.AdditionalServices.Dtos;
using FluentValidation;

namespace ConferenceRoomApi.Application.AdditionalServices.Validators;

public sealed class CreateAdditionalServiceRequestValidator : AbstractValidator<CreateAdditionalServiceRequest>
{
    public CreateAdditionalServiceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateAdditionalServiceRequestValidator : AbstractValidator<UpdateAdditionalServiceRequest>
{
    public UpdateAdditionalServiceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}
