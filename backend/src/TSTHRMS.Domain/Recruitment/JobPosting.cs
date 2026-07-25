using TSTHRMS.Domain.Common;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Tenancy;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Section 1: the public-facing listing, published from an approved <see cref="JobRequisition"/>
/// with one click (Section 2 - "no duplicate manual posting"). <see cref="Slug"/> gives each job
/// its own SEO-friendly URL.
/// </summary>
public class JobPosting : TenantScopedEntity
{
    public Guid JobRequisitionId { get; set; }
    public JobRequisition? JobRequisition { get; set; }

    public required string Title { get; set; }

    /// <summary>Unique per tenant - forms the public job URL (/careers/{tenantSlug}/{slug}).</summary>
    public required string Slug { get; set; }
    public required string Description { get; set; }
    public string? Department { get; set; }
    public string? Location { get; set; }
    public EmploymentType EmploymentType { get; set; }

    public Guid LegalEntityId { get; set; }
    public LegalEntity? LegalEntity { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    // Section 6: "not every role needs a test" - a per-posting, toggle-able config rather than a
    // fixed pipeline step. Kept on JobPosting itself (Build Notes: "keep the test configuration
    // attached to the job opening, not the candidate") rather than a separate config table, the
    // same way JobRequisition.InterviewRoundCount is inlined instead of split out.
    public bool IsAssessmentEnabled { get; set; }
    public AssessmentType AssessmentType { get; set; }
    public string? AssessmentInstructions { get; set; }
    public int AssessmentTimeLimitMinutes { get; set; } = 60;

    /// <summary>Days the candidate has to start and submit from when the test is sent - the
    /// PDF's "clear deadline", distinct from the time-boxed limit once they begin.</summary>
    public int AssessmentResponseWindowDays { get; set; } = 5;
    public int AssessmentPassThreshold { get; set; } = 60;
    public int AssessmentRetakeCooldownMonths { get; set; } = 6;

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
}

public enum AssessmentType
{
    MachineCodingTest,
    SkillAssignment,
    AptitudeTest,
    CaseStudy
}
