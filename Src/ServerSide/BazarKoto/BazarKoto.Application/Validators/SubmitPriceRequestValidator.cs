using BazarKoto.Contracts.Prices;
using FluentValidation;

namespace BazarKoto.Application.Validators;

public class SubmitPriceRequestValidator : AbstractValidator<SubmitPriceRequest>
{
    public SubmitPriceRequestValidator()
    {
        RuleFor(x => x.MarketId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Unit).NotEmpty();
        RuleFor(x => x.PricePerUnit).GreaterThan(0);
        RuleFor(x => x.PriceDate).NotEmpty();
        RuleFor(x => x.SellerType).NotEmpty();
        RuleFor(x => x.PriceSource).NotEmpty();
        RuleFor(x => x.QualityGrade).NotEmpty();
    }
}
