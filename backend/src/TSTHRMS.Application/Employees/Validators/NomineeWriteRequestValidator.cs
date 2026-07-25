using FluentValidation;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Application.Employees.Validators;

public class NomineeWriteRequestValidator : AbstractValidator<NomineeWriteRequest>
{
    public NomineeWriteRequestValidator()
    {
        RuleFor(x => x.NominationType).IsInEnum();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Relation).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ContactNumber).MaximumLength(30);

        RuleFor(x => x.SharePercentage)
            .InclusiveBetween(0.01m, 100m)
            .When(x => x.SharePercentage is not null)
            .WithMessage("Share percentage must be between 0 and 100.");

        RuleFor(x => x.FamilyMemberId)
            .NotEqual(Guid.Empty)
            .When(x => x.FamilyMemberId is not null)
            .WithMessage("Select a valid family member.");
    }
}
