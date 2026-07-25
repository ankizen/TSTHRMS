using FluentValidation;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Application.Employees.Validators;

public class ConfirmEmployeeRequestValidator : AbstractValidator<ConfirmEmployeeRequest>
{
    public ConfirmEmployeeRequestValidator()
    {
        RuleFor(x => x.ConfirmingManagerId).NotEqual(Guid.Empty).WithMessage("Select a confirming manager.");
    }
}
