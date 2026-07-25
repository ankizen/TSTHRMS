using FluentValidation;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment.Validators;

public class MoveApplicationStageRequestValidator : AbstractValidator<MoveApplicationStageRequest>
{
    public MoveApplicationStageRequestValidator()
    {
        RuleFor(x => x.Stage).IsInEnum();
        RuleFor(x => x.Reason).NotEmpty()
            .When(x => x.Stage is ApplicationStage.Rejected or ApplicationStage.OnHold)
            .WithMessage("A reason is required when rejecting or holding a candidate.");
    }
}
