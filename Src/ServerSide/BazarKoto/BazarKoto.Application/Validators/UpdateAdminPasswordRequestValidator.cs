using BazarKoto.Contracts.Auth;
using FluentValidation;

namespace BazarKoto.Application.Validators;

public class UpdateAdminPasswordRequestValidator : AbstractValidator<UpdateAdminPasswordRequest>
{
    public UpdateAdminPasswordRequestValidator()
    {
        RuleFor(x => x.OldPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(12);
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .Equal(x => x.NewPassword)
            .WithMessage("Confirm password must match new password.");
    }
}
