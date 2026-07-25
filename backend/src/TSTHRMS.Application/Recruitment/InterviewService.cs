using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Application.Users;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

/// <summary>
/// Section 7: scheduling, rescheduling/no-show tracking, and structured scorecards whose
/// visibility is gated until every assigned panelist has submitted (Section 14's blind-panel
/// requirement) - except a panelist's own scorecard and HRAdmin/HRBP, who always see everything.
/// </summary>
public class InterviewService(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IUserDirectory userDirectory,
    IUserManagementService userManagementService,
    IEmailSender emailSender,
    ILogger<InterviewService> logger) : IInterviewService
{
    public async Task<InterviewDto?> ScheduleAsync(
        Guid applicationId, ScheduleInterviewRequest request, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessApplicationAsync(applicationId, cancellationToken))
        {
            return null;
        }

        var interview = new Interview
        {
            ApplicationId = applicationId,
            Round = request.Round,
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = request.DurationMinutes,
            VideoLink = request.VideoLink,
            ScheduledByUserId = currentUserService.UserId ?? Guid.Empty,
        };
        dbContext.Interviews.Add(interview);

        foreach (var panelistUserId in request.PanelistUserIds.Distinct())
        {
            dbContext.InterviewPanelists.Add(new InterviewPanelist
            {
                InterviewId = interview.Id,
                InterviewerUserId = panelistUserId,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await SendCandidateEmailAsync(applicationId, interview, "Interview scheduled", cancellationToken);

        return await MapInterviewAsync(interview.Id, cancellationToken);
    }

    public async Task<InterviewDto?> RescheduleAsync(
        Guid interviewId, RescheduleInterviewRequest request, CancellationToken cancellationToken = default)
    {
        var interview = await dbContext.Interviews.FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken);
        if (interview is null || !await CanAccessApplicationAsync(interview.ApplicationId, cancellationToken))
        {
            return null;
        }

        interview.ScheduledAt = request.ScheduledAt;
        interview.RescheduleCount++;
        interview.Status = InterviewStatus.Scheduled;

        await dbContext.SaveChangesAsync(cancellationToken);

        await SendCandidateEmailAsync(interview.ApplicationId, interview, "Interview rescheduled", cancellationToken);

        return await MapInterviewAsync(interview.Id, cancellationToken);
    }

    public async Task<InterviewDto?> UpdateStatusAsync(
        Guid interviewId, UpdateInterviewStatusRequest request, CancellationToken cancellationToken = default)
    {
        var interview = await dbContext.Interviews.FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken);
        if (interview is null || !await CanAccessApplicationAsync(interview.ApplicationId, cancellationToken))
        {
            return null;
        }

        interview.Status = request.Status;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await MapInterviewAsync(interview.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<InterviewDto>?> GetForApplicationAsync(
        Guid applicationId, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessApplicationAsync(applicationId, cancellationToken))
        {
            return null;
        }

        var interviews = await dbContext.Interviews.AsNoTracking()
            .Where(i => i.ApplicationId == applicationId)
            .OrderBy(i => i.ScheduledAt)
            .Include(i => i.Panelists)
            .Include(i => i.Scorecards)
            .ToListAsync(cancellationToken);

        return await MapInterviewsAsync(interviews, cancellationToken);
    }

    public async Task<IReadOnlyList<MyInterviewDto>> GetMyInterviewsAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId ?? Guid.Empty;

        var interviews = await dbContext.InterviewPanelists.AsNoTracking()
            .Where(p => p.InterviewerUserId == userId)
            .Select(p => p.Interview!)
            .OrderBy(i => i.ScheduledAt)
            .Select(i => new
            {
                i.Id,
                i.ApplicationId,
                CandidateName = i.Application!.Candidate!.FirstName + " " + i.Application.Candidate.LastName,
                JobPostingTitle = i.Application.JobPosting!.Title,
                i.Round,
                i.ScheduledAt,
                i.DurationMinutes,
                i.VideoLink,
                i.Status,
                HasSubmitted = i.Scorecards.Any(s => s.InterviewerUserId == userId),
            })
            .ToListAsync(cancellationToken);

        return interviews
            .Select(i => new MyInterviewDto(
                i.Id, i.ApplicationId, i.CandidateName, i.JobPostingTitle, i.Round, i.ScheduledAt,
                i.DurationMinutes, i.VideoLink, i.Status, i.HasSubmitted))
            .ToList();
    }

    public async Task<InterviewScorecardDto?> SubmitScorecardAsync(
        Guid interviewId, SubmitScorecardRequest request, CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId ?? Guid.Empty;

        var isPanelist = await dbContext.InterviewPanelists
            .AnyAsync(p => p.InterviewId == interviewId && p.InterviewerUserId == userId, cancellationToken);
        if (!isPanelist)
        {
            return null;
        }

        var alreadySubmitted = await dbContext.InterviewScorecards
            .AnyAsync(s => s.InterviewId == interviewId && s.InterviewerUserId == userId, cancellationToken);
        if (alreadySubmitted)
        {
            return null;
        }

        var scorecard = new InterviewScorecard
        {
            InterviewId = interviewId,
            InterviewerUserId = userId,
            TechnicalSkillsRating = request.TechnicalSkillsRating,
            CommunicationRating = request.CommunicationRating,
            ProblemSolvingRating = request.ProblemSolvingRating,
            CultureFitRating = request.CultureFitRating,
            Recommendation = request.Recommendation,
            Comments = request.Comments,
            SubmittedAt = DateTimeOffset.UtcNow,
        };
        dbContext.InterviewScorecards.Add(scorecard);
        await dbContext.SaveChangesAsync(cancellationToken);

        var displayNames = await userDirectory.GetDisplayNamesAsync([userId], cancellationToken);
        return new InterviewScorecardDto(
            userId, displayNames.GetValueOrDefault(userId, "You"), scorecard.TechnicalSkillsRating,
            scorecard.CommunicationRating, scorecard.ProblemSolvingRating, scorecard.CultureFitRating,
            scorecard.Recommendation, scorecard.Comments, scorecard.SubmittedAt);
    }

    public async Task<IReadOnlyList<InterviewerCandidateDto>> GetInterviewerCandidatesAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await userManagementService.GetListAsync(cancellationToken);
        return users.Select(u => new InterviewerCandidateDto(u.Id, u.Email, u.EmployeeName)).ToList();
    }

    private async Task<InterviewDto?> MapInterviewAsync(Guid interviewId, CancellationToken cancellationToken)
    {
        var interview = await dbContext.Interviews.AsNoTracking()
            .Include(i => i.Panelists)
            .Include(i => i.Scorecards)
            .FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken);

        if (interview is null)
        {
            return null;
        }

        var dtos = await MapInterviewsAsync([interview], cancellationToken);
        return dtos[0];
    }

    private async Task<IReadOnlyList<InterviewDto>> MapInterviewsAsync(
        IReadOnlyList<Interview> interviews, CancellationToken cancellationToken)
    {
        var allUserIds = interviews
            .SelectMany(i => i.Panelists.Select(p => p.InterviewerUserId)
                .Concat(i.Scorecards.Select(s => s.InterviewerUserId)))
            .Distinct()
            .ToList();
        var displayNames = await userDirectory.GetDisplayNamesAsync(allUserIds, cancellationToken);

        var currentUserId = currentUserService.UserId ?? Guid.Empty;
        var isHr = currentUserService.Roles.Contains(RoleNames.HRAdmin) || currentUserService.Roles.Contains(RoleNames.HRBP);

        return interviews.Select(interview =>
        {
            var isPanelist = interview.Panelists.Any(p => p.InterviewerUserId == currentUserId);
            var hasSubmitted = interview.Scorecards.Any(s => s.InterviewerUserId == currentUserId);
            var allSubmitted = interview.Panelists.Count > 0
                && interview.Panelists.All(p => interview.Scorecards.Any(s => s.InterviewerUserId == p.InterviewerUserId));

            var canSeeAll = isHr || allSubmitted;

            var visibleScorecards = interview.Scorecards
                .Where(s => canSeeAll || s.InterviewerUserId == currentUserId)
                .Select(s => new InterviewScorecardDto(
                    s.InterviewerUserId, displayNames.GetValueOrDefault(s.InterviewerUserId, "Unknown"),
                    s.TechnicalSkillsRating, s.CommunicationRating, s.ProblemSolvingRating, s.CultureFitRating,
                    s.Recommendation, s.Comments, s.SubmittedAt))
                .ToList();

            var panelistDtos = interview.Panelists
                .Select(p => new InterviewPanelistDto(
                    p.InterviewerUserId, displayNames.GetValueOrDefault(p.InterviewerUserId, "Unknown"),
                    interview.Scorecards.Any(s => s.InterviewerUserId == p.InterviewerUserId)))
                .ToList();

            return new InterviewDto(
                interview.Id, interview.ApplicationId, interview.Round, interview.ScheduledAt,
                interview.DurationMinutes, interview.VideoLink, interview.Status, interview.RescheduleCount,
                panelistDtos, visibleScorecards, allSubmitted, isPanelist, hasSubmitted);
        }).ToList();
    }

    private async Task SendCandidateEmailAsync(
        Guid applicationId, Interview interview, string subjectPrefix, CancellationToken cancellationToken)
    {
        try
        {
            var details = await dbContext.Applications.AsNoTracking()
                .Where(a => a.Id == applicationId)
                .Select(a => new { a.Candidate!.FirstName, a.Candidate.Email, JobTitle = a.JobPosting!.Title })
                .FirstOrDefaultAsync(cancellationToken);

            if (details is null)
            {
                return;
            }

            var videoLinkLine = string.IsNullOrWhiteSpace(interview.VideoLink)
                ? ""
                : $"<p>Join here: <a href=\"{System.Net.WebUtility.HtmlEncode(interview.VideoLink)}\">{System.Net.WebUtility.HtmlEncode(interview.VideoLink)}</a></p>";

            var html = $"""
                <p>Hi {System.Net.WebUtility.HtmlEncode(details.FirstName)},</p>
                <p>Your interview for <strong>{System.Net.WebUtility.HtmlEncode(details.JobTitle)}</strong> is scheduled for
                {interview.ScheduledAt:f} ({interview.DurationMinutes} minutes).</p>
                {videoLinkLine}
                """;

            await emailSender.SendAsync(details.Email, $"{subjectPrefix}: {details.JobTitle}", html, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send interview email for application {ApplicationId}", applicationId);
        }
    }

    private async Task<bool> CanAccessApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var raisedByUserId = await dbContext.Applications.AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => (Guid?)a.JobPosting!.JobRequisition!.RaisedByUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (raisedByUserId is null)
        {
            return false;
        }

        if (currentUserService.Roles.Contains(RoleNames.HRAdmin) || currentUserService.Roles.Contains(RoleNames.HRBP))
        {
            return true;
        }

        return raisedByUserId == (currentUserService.UserId ?? Guid.Empty);
    }
}
