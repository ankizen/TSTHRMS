using FluentValidation;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment.Validators;

public class JobRequisitionWriteRequestValidator : AbstractValidator<JobRequisitionWriteRequest>
{
    public JobRequisitionWriteRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LegalEntityId).NotEqual(Guid.Empty).WithMessage("Legal entity is required.");
        RuleFor(x => x.ProductId).NotEqual(Guid.Empty).WithMessage("Product is required.");
        RuleFor(x => x.EmploymentType).IsInEnum();
        RuleFor(x => x.Reason).IsInEnum();
        RuleFor(x => x.Openings).GreaterThan(0);
        RuleFor(x => x.InterviewRoundCount).InclusiveBetween(1, 6)
            .WithMessage("Interview rounds must be between 1 and 6.");
        RuleFor(x => x.BudgetPerOpening).GreaterThanOrEqualTo(0)
            .When(x => x.BudgetPerOpening is not null);
        RuleFor(x => x.Grade).MaximumLength(50);
        RuleFor(x => x.Department).MaximumLength(100);
    }
}
