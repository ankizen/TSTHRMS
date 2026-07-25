using FluentValidation;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment.Validators;

public class ScoreAssessmentRequestValidator : AbstractValidator<ScoreAssessmentRequest>
{
    public ScoreAssessmentRequestValidator()
    {
        RuleFor(x => x.Score).InclusiveBetween(0, 100);
        RuleFor(x => x.Comments).MaximumLength(2000);
    }
}
