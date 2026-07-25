using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

/// <summary>
/// Section 5's internal pipeline view, scoped to one job posting at a time. Stage moves are
/// recorded in ApplicationStageHistory (append-only, per the Build Notes) rather than just
/// overwriting Application.Stage. Slice 2 adds cross-posting duplicate surfacing (Section 3's
/// "duplicate detection" from the other direction) and browsing the Section 5 talent pool.
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

        var applications = await dbContext.Applications.AsNoTracking()
            .Where(a => a.JobPostingId == jobPostingId)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new
            {
                a.Id, a.CandidateId, a.Candidate!.FirstName, a.Candidate.LastName, a.Candidate.Email,
                a.Candidate.Phone, a.Candidate.ResumeDocumentId, a.Candidate.CurrentCtc, a.Candidate.ExpectedCtc,
                a.Candidate.NoticePeriodDays, a.Candidate.Source, a.Candidate.IsInTalentPool, a.Stage,
                a.StageChangedAt, a.RejectionReason, a.AppliedAt,
            })
            .ToListAsync(cancellationToken);

        var candidateIds = applications.Select(a => a.CandidateId).ToList();
        var otherApplicationsByCandidate = await GetOtherApplicationsByCandidateAsync(
            candidateIds, jobPostingId, cancellationToken);
        var assessmentsByApplication = await GetAssessmentSummariesByApplicationAsync(
            applications.Select(a => a.Id).ToList(), cancellationToken);

        return applications
            .Select(a => new ApplicantListItemDto(
                a.Id, a.CandidateId, a.FirstName, a.LastName, a.Email, a.Phone, a.ResumeDocumentId,
                a.CurrentCtc, a.ExpectedCtc, a.NoticePeriodDays, a.Source, a.IsInTalentPool, a.Stage,
                a.StageChangedAt, a.RejectionReason, a.AppliedAt,
                otherApplicationsByCandidate.GetValueOrDefault(a.CandidateId, []),
                assessmentsByApplication.GetValueOrDefault(a.Id)))
            .ToList();
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
        var otherApplications = await GetOtherApplicationsByCandidateAsync(
            [candidate.Id], application.JobPostingId, cancellationToken);
        var assessments = await GetAssessmentSummariesByApplicationAsync([application.Id], cancellationToken);

        return new ApplicantListItemDto(
            application.Id, candidate.Id, candidate.FirstName, candidate.LastName, candidate.Email,
            candidate.Phone, candidate.ResumeDocumentId, candidate.CurrentCtc, candidate.ExpectedCtc,
            candidate.NoticePeriodDays, candidate.Source, candidate.IsInTalentPool, application.Stage,
            application.StageChangedAt, application.RejectionReason, application.AppliedAt,
            otherApplications.GetValueOrDefault(candidate.Id, []),
            assessments.GetValueOrDefault(application.Id));
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

    public async Task<IReadOnlyList<TalentPoolCandidateDto>> GetTalentPoolAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await dbContext.Candidates.AsNoTracking()
            .Where(c => c.IsInTalentPool)
            .Select(c => new
            {
                c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.ResumeDocumentId,
                MostRecent = c.Applications
                    .OrderByDescending(a => a.AppliedAt)
                    .Select(a => new { a.JobPosting!.Title, a.Stage, a.AppliedAt })
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return candidates
            .Select(c => new TalentPoolCandidateDto(
                c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.ResumeDocumentId,
                c.MostRecent?.Title, c.MostRecent?.Stage, c.MostRecent?.AppliedAt))
            .ToList();
    }

    private async Task<Dictionary<Guid, IReadOnlyList<CandidateOtherApplicationDto>>> GetOtherApplicationsByCandidateAsync(
        IReadOnlyList<Guid> candidateIds, Guid excludingJobPostingId, CancellationToken cancellationToken)
    {
        if (candidateIds.Count == 0)
        {
            return [];
        }

        var others = await dbContext.Applications.AsNoTracking()
            .Where(a => candidateIds.Contains(a.CandidateId) && a.JobPostingId != excludingJobPostingId)
            .Select(a => new
            {
                a.CandidateId,
                Other = new CandidateOtherApplicationDto(a.Id, a.JobPostingId, a.JobPosting!.Title, a.Stage, a.AppliedAt),
            })
            .ToListAsync(cancellationToken);

        return others
            .GroupBy(x => x.CandidateId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<CandidateOtherApplicationDto>)g.Select(x => x.Other).ToList());
    }

    private async Task<Dictionary<Guid, AssessmentSummaryDto>> GetAssessmentSummariesByApplicationAsync(
        IReadOnlyList<Guid> applicationIds, CancellationToken cancellationToken)
    {
        if (applicationIds.Count == 0)
        {
            return [];
        }

        var assessments = await dbContext.AssessmentSubmissions.AsNoTracking()
            .Where(a => applicationIds.Contains(a.ApplicationId))
            .Select(a => new
            {
                a.ApplicationId, a.Id, AssessmentType = a.Application!.JobPosting!.AssessmentType,
                a.SentAt, a.DueAt, a.SubmittedAt, a.Score, a.Passed, a.RetakeAllowedAfter,
            })
            .ToListAsync(cancellationToken);

        return assessments.ToDictionary(
            a => a.ApplicationId,
            a => new AssessmentSummaryDto(
                a.Id, a.AssessmentType, a.SentAt, a.DueAt, a.SubmittedAt, a.Score, a.Passed, a.RetakeAllowedAfter));
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
