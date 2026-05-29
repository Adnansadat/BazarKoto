using BazarKoto.Contracts.Contact;
using FluentValidation;

namespace BazarKoto.Application.Validators;

public class CreateContactMessageRequestValidator : AbstractValidator<CreateContactMessageRequest>
{
    public CreateContactMessageRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(80);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(120);

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(150);

        RuleFor(x => x.Message)
            .NotEmpty()
            .MinimumLength(20)
            .MaximumLength(2000);
    }
}
