using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment;

public class CandidatePortalService(IApplicationDbContext dbContext, ICandidateContext candidateContext) : ICandidatePortalService
{
    public async Task<IReadOnlyList<MyApplicationDto>> GetMyApplicationsAsync(CancellationToken cancellationToken = default)
    {
        var candidateId = candidateContext.CandidateId;
        if (candidateId is null)
        {
            return [];
        }

        var applications = await dbContext.Applications.AsNoTracking()
            .Where(a => a.CandidateId == candidateId)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new { a.Id, JobPostingTitle = a.JobPosting!.Title, a.Stage, a.AppliedAt })
            .ToListAsync(cancellationToken);

        var applicationIds = applications.Select(a => a.Id).ToList();
        if (applicationIds.Count == 0)
        {
            return [];
        }

        var interviewsByApplication = (await dbContext.Interviews.AsNoTracking()
                .Where(i => applicationIds.Contains(i.ApplicationId))
                .OrderBy(i => i.ScheduledAt)
                .Select(i => new
                {
                    i.ApplicationId,
                    Interview = new MyApplicationInterviewDto(i.Id, i.Round, i.ScheduledAt, i.DurationMinutes, i.VideoLink, i.Status),
                })
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.ApplicationId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<MyApplicationInterviewDto>)g.Select(x => x.Interview).ToList());

        var assessmentsByApplication = await dbContext.AssessmentSubmissions.AsNoTracking()
            .Where(s => applicationIds.Contains(s.ApplicationId))
            .Select(s => new
            {
                s.ApplicationId,
                Assessment = new MyApplicationAssessmentDto(s.Application!.JobPosting!.AssessmentType, s.DueAt, s.SubmittedAt != null),
            })
            .ToDictionaryAsync(x => x.ApplicationId, x => x.Assessment, cancellationToken);

        var offersByApplication = await dbContext.Offers.AsNoTracking()
            .Where(o => applicationIds.Contains(o.ApplicationId))
            .Select(o => new
            {
                o.ApplicationId,
                Offer = new MyApplicationOfferDto(o.Status, o.Status == Domain.Recruitment.OfferStatus.Sent ? o.Token : null),
            })
            .ToDictionaryAsync(x => x.ApplicationId, x => x.Offer, cancellationToken);

        return applications
            .Select(a => new MyApplicationDto(
                a.Id, a.JobPostingTitle, a.Stage, a.AppliedAt,
                interviewsByApplication.GetValueOrDefault(a.Id, []),
                assessmentsByApplication.GetValueOrDefault(a.Id),
                offersByApplication.GetValueOrDefault(a.Id)))
            .ToList();
    }
}
