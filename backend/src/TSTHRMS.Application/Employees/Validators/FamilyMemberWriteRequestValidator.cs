using FluentValidation;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Application.Employees.Validators;

public class FamilyMemberWriteRequestValidator : AbstractValidator<FamilyMemberWriteRequest>
{
    public FamilyMemberWriteRequestValidator()
    {
        RuleFor(x => x.Relation).IsInEnum();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.DateOfBirth is not null)
            .WithMessage("Date of birth cannot be in the future.");
    }
}
