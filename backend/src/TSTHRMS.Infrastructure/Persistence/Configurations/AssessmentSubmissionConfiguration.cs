using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class AssessmentSubmissionConfiguration : IEntityTypeConfiguration<AssessmentSubmission>
{
    public void Configure(EntityTypeBuilder<AssessmentSubmission> builder)
    {
        builder.ToTable("AssessmentSubmissions");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Token).IsRequired().HasMaxLength(64);
        builder.HasIndex(a => a.Token).IsUnique();

        // At most one assessment attempt per application - a real retake is a new
        // application/posting later, not a second row here.
        builder.HasIndex(a => new { a.TenantId, a.ApplicationId }).IsUnique();

        builder.Property(a => a.SubmissionText).HasColumnType("text");
        builder.Property(a => a.ReviewerComments).HasColumnType("text");

        builder.HasOne(a => a.Application)
            .WithMany()
            .HasForeignKey(a => a.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.SubmissionDocument)
            .WithMany()
            .HasForeignKey(a => a.SubmissionDocumentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
