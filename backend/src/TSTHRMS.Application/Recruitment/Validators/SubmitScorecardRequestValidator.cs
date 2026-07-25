using FluentValidation;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment.Validators;

public class SubmitScorecardRequestValidator : AbstractValidator<SubmitScorecardRequest>
{
    public SubmitScorecardRequestValidator()
    {
        RuleFor(x => x.TechnicalSkillsRating).InclusiveBetween(1, 5);
        RuleFor(x => x.CommunicationRating).InclusiveBetween(1, 5);
        RuleFor(x => x.ProblemSolvingRating).InclusiveBetween(1, 5);
        RuleFor(x => x.CultureFitRating).InclusiveBetween(1, 5);
        RuleFor(x => x.Recommendation).IsInEnum();
        RuleFor(x => x.Comments).MaximumLength(2000);
    }
}
