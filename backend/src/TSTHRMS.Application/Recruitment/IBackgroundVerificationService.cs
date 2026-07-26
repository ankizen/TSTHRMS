using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment;

/// <summary>Section 9. Deliberately stays queryable on the application indefinitely, even past
/// Hired, since the PDF's own "why this matters" note is that a post-joining discrepancy needs
/// somewhere to still show up rather than disappearing once the hire is made.</summary>
public interface IBackgroundVerificationService
{
    /// <summary>Null means not found/no access. Returns a default NotStarted-state DTO (not
    /// null) when the caller has access but nothing has been initiated yet.</summary>
    Task<BgvDto?> GetForApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);

    Task<BgvDto?> InitiateAsync(
        Guid applicationId, InitiateBgvRequest request, CancellationToken cancellationToken = default);

    /// <summary>Null also means nothing has been initiated yet - there's no status to update.</summary>
    Task<BgvDto?> UpdateStatusAsync(
        Guid applicationId, UpdateBgvStatusRequest request, CancellationToken cancellationToken = default);
}
