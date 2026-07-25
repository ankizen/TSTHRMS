using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

/// <summary>
/// Section 5's internal pipeline view, scoped to one job posting at a time. Stage moves are
/// recorded in ApplicationStageHistory (append-only, per the Build Notes) rather than just
/// overwriting Application.Stage - the full kanban-style board and cross-posting duplicate
/// surfacing are a later slice; this is the minimal end-to-end "HR can see and move applicants"
/// screen for Slice 1.
/// </summary>
public class ApplicantService(IApplicationDbContext dbContext, ICurrentUserService currentUserService) : IApplicantService
{
    public async Task<IReadOnlyList<ApplicantListItemDto>?> GetForPostingAsync(
        Guid jobPostingId, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessPostingAsync(jobPostingId, cancellationToken))
        {
            return null;
        }

        return await dbContext.Applications.AsNoTracking()
            .Where(a => a.JobPostingId == jobPostingId)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new ApplicantListItemDto(
                a.Id, a.CandidateId, a.Candidate!.FirstName, a.Candidate.LastName, a.Candidate.Email,
                a.Candidate.Phone, a.Candidate.ResumeDocumentId, a.Candidate.CurrentCtc, a.Candidate.ExpectedCtc,
                a.Candidate.NoticePeriodDays, a.Candidate.Source, a.Candidate.IsInTalentPool, a.Stage,
                a.StageChangedAt, a.RejectionReason, a.AppliedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ApplicantListItemDto?> MoveStageAsync(
        Guid applicationId, MoveApplicationStageRequest request, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.Applications
            .Include(a => a.Candidate)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (application is null || !await CanAccessPostingAsync(application.JobPostingId, cancellationToken))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        dbContext.ApplicationStageHistories.Add(new ApplicationStageHistory
        {
            ApplicationId = application.Id,
            FromStage = application.Stage,
            ToStage = request.Stage,
            Reason = request.Reason,
            ChangedByUserId = currentUserService.UserId ?? Guid.Empty,
            ChangedAt = now,
        });

        application.Stage = request.Stage;
        application.StageChangedAt = now;
        application.RejectionReason = request.Stage == ApplicationStage.Rejected ? request.Reason : null;

        await dbContext.SaveChangesAsync(cancellationToken);

        var candidate = application.Candidate!;
        return new ApplicantListItemDto(
            application.Id, candidate.Id, candidate.FirstName, candidate.LastName, candidate.Email,
            candidate.Phone, candidate.ResumeDocumentId, candidate.CurrentCtc, candidate.ExpectedCtc,
            candidate.NoticePeriodDays, candidate.Source, candidate.IsInTalentPool, application.Stage,
            application.StageChangedAt, application.RejectionReason, application.AppliedAt);
    }

    public async Task<bool> SetTalentPoolAsync(
        Guid candidateId, bool isInTalentPool, CancellationToken cancellationToken = default)
    {
        var candidate = await dbContext.Candidates.FirstOrDefaultAsync(c => c.Id == candidateId, cancellationToken);
        if (candidate is null)
        {
            return false;
        }

        candidate.IsInTalentPool = isInTalentPool;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Section 14: a Manager (Hiring Manager) only sees candidates for their own open
    /// roles - i.e. postings whose requisition they raised. HRAdmin/HRBP see everything.</summary>
    private async Task<bool> CanAccessPostingAsync(Guid jobPostingId, CancellationToken cancellationToken)
    {
        var raisedByUserId = await dbContext.JobPostings.AsNoTracking()
            .Where(p => p.Id == jobPostingId)
            .Select(p => (Guid?)p.JobRequisition!.RaisedByUserId)
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
