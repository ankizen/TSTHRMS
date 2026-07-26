using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment;

public interface IRecruitmentReportingService
{
    /// <summary>Scoped the same way as JobRequisitionService.ApplyOwnerScope - a Manager
    /// (neither HRAdmin nor HRBP) only ever sees requisitions they raised and everything
    /// downstream of them (postings, applications, offers).</summary>
    Task<RecruitmentReportDto> GetReportAsync(CancellationToken cancellationToken = default);
}
