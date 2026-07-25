using FluentValidation;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Application.Employees.Validators;

public class PreviousEmploymentRecordWriteRequestValidator : AbstractValidator<PreviousEmploymentRecordWriteRequest>
{
    public PreviousEmploymentRecordWriteRequestValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Designation).MaximumLength(100);
        RuleFor(x => x.ReasonForLeaving).MaximumLength(500);
        RuleFor(x => x.PreviousUan).MaximumLength(20);

        RuleFor(x => x.YearsOfExperience)
            .GreaterThanOrEqualTo(0)
            .When(x => x.YearsOfExperience is not null)
            .WithMessage("Years of experience can't be negative.");

        RuleFor(x => x.DateOfJoining)
            .NotEqual(default(DateOnly))
            .WithMessage("Date of joining is required.");

        RuleFor(x => x.DateOfLeaving)
            .NotEqual(default(DateOnly))
            .WithMessage("Date of leaving is required.")
            .GreaterThan(x => x.DateOfJoining)
            .WithMessage("Date of leaving must be after the date of joining.");
    }
}
