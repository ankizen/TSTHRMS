using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("Applications");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Stage).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.RejectionReason).HasMaxLength(1000);

        builder.HasIndex(a => new { a.TenantId, a.JobPostingId, a.Stage });
        builder.HasIndex(a => new { a.TenantId, a.CandidateId });

        builder.HasOne(a => a.Candidate)
            .WithMany(c => c.Applications)
            .HasForeignKey(a => a.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.JobPosting)
            .WithMany(p => p.Applications)
            .HasForeignKey(a => a.JobPostingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
