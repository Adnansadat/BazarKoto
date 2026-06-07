using BazarKoto.Contracts.Auth;
using FluentValidation;

namespace BazarKoto.Application.Validators;

public class UpdateAdminCredentialsRequestValidator : AbstractValidator<UpdateAdminCredentialsRequest>
{
    public UpdateAdminCredentialsRequestValidator()
    {
        RuleFor(x => x.OldEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.NewEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.OldPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(12);
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .Equal(x => x.NewPassword)
            .WithMessage("Confirm password must match new password.");
    }
}
