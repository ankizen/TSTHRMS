using FluentValidation;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Application.Employees.Validators;

public class SubmitEditRequestsRequestValidator : AbstractValidator<SubmitEditRequestsRequest>
{
    public SubmitEditRequestsRequestValidator()
    {
        RuleFor(x => x.Changes).NotEmpty();
        RuleForEach(x => x.Changes).ChildRules(change =>
        {
            change.RuleFor(c => c.Field).IsInEnum();
            change.RuleFor(c => c.NewValue).MaximumLength(500);
        });
    }
}
