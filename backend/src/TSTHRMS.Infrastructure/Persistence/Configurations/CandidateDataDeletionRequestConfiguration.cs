using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class CandidateDataDeletionRequestConfiguration : IEntityTypeConfiguration<CandidateDataDeletionRequest>
{
    public void Configure(EntityTypeBuilder<CandidateDataDeletionRequest> builder)
    {
        builder.ToTable("CandidateDataDeletionRequests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.HrDecisionNotes).HasColumnType("text");

        builder.HasIndex(r => new { r.TenantId, r.CandidateId });

        builder.HasOne(r => r.Candidate)
            .WithMany()
            .HasForeignKey(r => r.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
