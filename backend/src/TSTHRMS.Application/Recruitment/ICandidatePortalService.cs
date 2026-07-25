using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment;

/// <summary>Section 3: "own application status, upcoming interview slots, and documents
/// requested" - self-scoped entirely by ICandidateContext, never taking a candidate id as a
/// parameter, so there is no way to accidentally leak another candidate's data.</summary>
public interface ICandidatePortalService
{
    Task<IReadOnlyList<MyApplicationDto>> GetMyApplicationsAsync(CancellationToken cancellationToken = default);
}
