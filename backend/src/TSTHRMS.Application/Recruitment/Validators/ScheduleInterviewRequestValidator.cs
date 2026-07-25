using FluentValidation;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment.Validators;

public class ScheduleInterviewRequestValidator : AbstractValidator<ScheduleInterviewRequest>
{
    private static readonly ApplicationStage[] InterviewRounds =
        [ApplicationStage.InterviewRound1, ApplicationStage.InterviewRound2, ApplicationStage.InterviewRound3];

    public ScheduleInterviewRequestValidator()
    {
        RuleFor(x => x.Round).Must(round => InterviewRounds.Contains(round))
            .WithMessage("Round must be one of InterviewRound1, InterviewRound2, or InterviewRound3.");
        RuleFor(x => x.DurationMinutes).InclusiveBetween(15, 240);
        RuleFor(x => x.VideoLink).MaximumLength(500);
        RuleFor(x => x.PanelistUserIds).NotEmpty().WithMessage("At least one interviewer is required.");
    }
}
