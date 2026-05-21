using BazarKoto.Contracts.Markets;
using FluentValidation;

namespace BazarKoto.Application.Validators;

public class CreateMarketRequestValidator : AbstractValidator<CreateMarketRequest>
{
    public CreateMarketRequestValidator()
    {
        RuleFor(x => x.DivisionId).NotEmpty();
        RuleFor(x => x.DistrictId).NotEmpty();
        RuleFor(x => x.UpazilaId).NotEmpty();
        RuleFor(x => x.Area).NotEmpty();
        RuleFor(x => x.MarketName).NotEmpty();
        RuleFor(x => x.MarketType).NotEmpty();
        RuleFor(x => x.OperatingSchedule).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
