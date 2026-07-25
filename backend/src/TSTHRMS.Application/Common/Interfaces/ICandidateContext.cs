namespace TSTHRMS.Application.Common.Interfaces;

/// <summary>
/// Resolves the current candidate from a Candidate Portal JWT's candidate_id claim - the
/// candidate equivalent of ICurrentUserService, but for the separate, role-less token type
/// issued by GenerateCandidateAccessToken. Null means the caller isn't authenticated as a
/// candidate (either an anonymous request, or a staff JWT with no candidate_id claim).
/// </summary>
public interface ICandidateContext
{
    Guid? CandidateId { get; }
}
