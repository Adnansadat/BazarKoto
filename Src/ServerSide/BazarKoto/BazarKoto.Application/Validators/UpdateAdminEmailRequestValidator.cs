using BazarKoto.Contracts.Auth;
using FluentValidation;

namespace BazarKoto.Application.Validators;

public class UpdateAdminEmailRequestValidator : AbstractValidator<UpdateAdminEmailRequest>
{
    public UpdateAdminEmailRequestValidator()
    {
        RuleFor(x => x.NewEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.CurrentPassword).NotEmpty();
    }
}
