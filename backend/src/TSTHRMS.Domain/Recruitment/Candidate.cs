using TSTHRMS.Domain.Common;
using TSTHRMS.Domain.Documents;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Section 3: one row per person, regardless of how many jobs they apply to or which channel
/// they came from - <see cref="Application"/> is the per-job pipeline record. Deduped within a
/// tenant by (Email, Phone) on intake so a repeat applicant doesn't lose their history
/// (Section 3 - "duplicate detection").
/// </summary>
public class Candidate : TenantScopedEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }

    public Guid? ResumeDocumentId { get; set; }
    public Document? ResumeDocument { get; set; }

    public decimal? CurrentCtc { get; set; }
    public decimal? ExpectedCtc { get; set; }
    public int? NoticePeriodDays { get; set; }

    /// <summary>Section 3 - needed later for a cost/quality-per-source hiring report.</summary>
    public CandidateSource Source { get; set; }

    /// <summary>Section 4: set only when Source is Referral.</summary>
    public Guid? ReferredByEmployeeId { get; set; }
    public Employee? ReferredByEmployee { get; set; }

    /// <summary>Section 13 (DPDPA 2023): explicit consent captured on the application form.</summary>
    public DateTimeOffset ConsentGivenAt { get; set; }

    /// <summary>Section 5: rejected-but-good candidates tagged "Keep in mind" instead of lost.</summary>
    public bool IsInTalentPool { get; set; }

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
}

public enum CandidateSource
{
    CareerSite,
    Referral,
    LinkedIn,
    Naukri,
    Indeed,
    WalkIn,
    CampusDrive
}
