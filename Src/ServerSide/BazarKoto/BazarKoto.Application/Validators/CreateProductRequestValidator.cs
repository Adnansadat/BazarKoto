using BazarKoto.Contracts.Products;
using FluentValidation;

namespace BazarKoto.Application.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.NameEn).NotEmpty();
        RuleFor(x => x.NameBn).NotEmpty();
        RuleFor(x => x.LocalName).MaximumLength(200);
        RuleFor(x => x.PrimaryUnit).NotEmpty();
        RuleFor(x => x.ProductState).NotEmpty();
    }
}
