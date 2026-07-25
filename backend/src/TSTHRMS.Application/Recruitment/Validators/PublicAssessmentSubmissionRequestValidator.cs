using FluentValidation;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment.Validators;

public class PublicAssessmentSubmissionRequestValidator : AbstractValidator<PublicAssessmentSubmissionRequest>
{
    public PublicAssessmentSubmissionRequestValidator()
    {
        RuleFor(x => x.SubmissionText).NotEmpty().MaximumLength(20000);
    }
}
