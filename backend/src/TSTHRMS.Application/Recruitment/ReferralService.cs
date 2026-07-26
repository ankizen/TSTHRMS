using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

public class ReferralService(
    IApplicationDbContext dbContext, ICurrentUserService currentUserService, ICareerSiteService careerSiteService)
    : IReferralService
{
    public async Task<ApplyResult> SubmitReferralAsync(
        string jobSlug,
        ReferralSubmissionRequest request,
        Stream? resumeStream,
        string? resumeFileName,
        string? resumeContentType,
        long resumeSizeBytes,
        CancellationToken cancellationToken = default)
    {
        var employeeId = currentUserService.EmployeeId;
        if (employeeId is null)
        {
            return ApplyResult.Failure("Only a login linked to an employee record can refer a candidate.");
        }

        var applyRequest = new PublicApplicationRequest(
            request.FirstName, request.LastName, request.Email, request.Phone, null, null, null, true);

        return await careerSiteService.ApplyAsync(
            jobSlug, applyRequest, CandidateSource.Referral, employeeId,
            resumeStream, resumeFileName, resumeContentType, resumeSizeBytes, cancellationToken);
    }

    public async Task<IReadOnlyList<MyReferralDto>> GetMyReferralsAsync(CancellationToken cancellationToken = default)
    {
        var employeeId = currentUserService.EmployeeId;
        if (employeeId is null)
        {
            return [];
        }

        return await dbContext.Candidates.AsNoTracking()
            .Where(c => c.ReferredByEmployeeId == employeeId)
            .SelectMany(c => c.Applications.Select(a => new
            {
                CandidateId = c.Id,
                CandidateName = c.FirstName + " " + c.LastName,
                JobPostingTitle = a.JobPosting!.Title,
                a.Stage,
                a.AppliedAt,
                c.ReferralBonusStatus,
                c.ReferralBonusAmount,
            }))
            .OrderByDescending(r => r.AppliedAt)
            .Select(r => new MyReferralDto(
                r.CandidateId, r.CandidateName, r.JobPostingTitle, r.Stage, r.AppliedAt,
                r.ReferralBonusStatus, r.ReferralBonusAmount))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReferralPayoutDto>> GetPayoutsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Candidates.AsNoTracking()
            .Where(c => c.ReferralBonusStatus != ReferralBonusStatus.NotApplicable)
            .Select(c => new
            {
                CandidateId = c.Id,
                CandidateName = c.FirstName + " " + c.LastName,
                ReferredByEmployeeName = c.ReferredByEmployee!.FirstName + " " + c.ReferredByEmployee.LastName,
                JobPostingTitle = c.Applications.OrderByDescending(a => a.AppliedAt).First().JobPosting!.Title,
                BonusAmount = c.ReferralBonusAmount!.Value,
                c.ReferralBonusStatus,
                c.ReferralBonusPaidAt,
            })
            .OrderBy(r => r.ReferralBonusStatus == ReferralBonusStatus.Payable ? 0 : 1)
            .ThenByDescending(r => r.ReferralBonusPaidAt)
            .Select(r => new ReferralPayoutDto(
                r.CandidateId, r.CandidateName, r.ReferredByEmployeeName, r.JobPostingTitle,
                r.BonusAmount, r.ReferralBonusStatus, r.ReferralBonusPaidAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkBonusPaidAsync(Guid candidateId, CancellationToken cancellationToken = default)
    {
        var candidate = await dbContext.Candidates.FirstOrDefaultAsync(c => c.Id == candidateId, cancellationToken);
        if (candidate is null || candidate.ReferralBonusStatus != ReferralBonusStatus.Payable)
        {
            return false;
        }

        candidate.ReferralBonusStatus = ReferralBonusStatus.Paid;
        candidate.ReferralBonusPaidAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
