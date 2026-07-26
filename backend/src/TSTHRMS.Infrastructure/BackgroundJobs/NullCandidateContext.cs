using TSTHRMS.Application.Common.Interfaces;

namespace TSTHRMS.Infrastructure.BackgroundJobs;

/// <summary>ICandidateContext for a background job - never running on behalf of a candidate
/// session, so there's nothing to resolve.</summary>
public class NullCandidateContext : ICandidateContext
{
    public Guid? CandidateId => null;
}
