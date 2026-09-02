using ConferenceRoomApi.Application.Reports.Dtos;
using FluentValidation;

namespace ConferenceRoomApi.Application.Reports.Validators;

public sealed class DateRangeRequestValidator : AbstractValidator<DateRangeRequest>
{
    public DateRangeRequestValidator()
    {
        RuleFor(x => x.From).LessThanOrEqualTo(x => x.To).WithMessage("From must not be after To.");
    }
}

public sealed class RevenueReportRequestValidator : AbstractValidator<RevenueReportRequest>
{
    public RevenueReportRequestValidator()
    {
        RuleFor(x => x.From).LessThanOrEqualTo(x => x.To).WithMessage("From must not be after To.");
    }
}
