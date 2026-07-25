using FluentValidation;
using TSTHRMS.Application.Users.Dtos;

namespace TSTHRMS.Application.Users.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEqual(Guid.Empty).WithMessage("Employee is required.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(10);
        RuleFor(x => x.Role).NotEmpty();
    }
}
