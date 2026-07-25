using FluentValidation;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment.Validators;

public class CreateOrReviseOfferRequestValidator : AbstractValidator<CreateOrReviseOfferRequest>
{
    public CreateOrReviseOfferRequestValidator()
    {
        RuleFor(x => x.Designation).MaximumLength(100);
        RuleFor(x => x.DateOfJoining).NotEqual(default(DateOnly)).WithMessage("Date of joining is required.");
        RuleFor(x => x.AnnualCtc).GreaterThan(0);
        RuleFor(x => x.FixedComponent).GreaterThanOrEqualTo(0).When(x => x.FixedComponent is not null);
        RuleFor(x => x.VariableComponent).GreaterThanOrEqualTo(0).When(x => x.VariableComponent is not null);
        RuleFor(x => x.JoiningBonus).GreaterThanOrEqualTo(0).When(x => x.JoiningBonus is not null);
        RuleFor(x => x.RevisionReason).MaximumLength(1000);
    }
}
