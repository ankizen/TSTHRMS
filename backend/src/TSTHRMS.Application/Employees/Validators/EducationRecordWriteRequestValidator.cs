using FluentValidation;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Application.Employees.Validators;

public class EducationRecordWriteRequestValidator : AbstractValidator<EducationRecordWriteRequest>
{
    public EducationRecordWriteRequestValidator()
    {
        RuleFor(x => x.QualificationLevel).IsInEnum();
        RuleFor(x => x.DegreeName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.InstituteName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Specialization).MaximumLength(200);
        RuleFor(x => x.YearOfPassing)
            .InclusiveBetween(1950, DateTime.UtcNow.Year)
            .WithMessage($"Year of passing must be between 1950 and {DateTime.UtcNow.Year}.");
    }
}
