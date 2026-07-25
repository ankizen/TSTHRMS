using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class CandidateOtpConfiguration : IEntityTypeConfiguration<CandidateOtp>
{
    public void Configure(EntityTypeBuilder<CandidateOtp> builder)
    {
        builder.ToTable("CandidateOtps");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CodeHash).IsRequired().HasMaxLength(128);
        builder.HasIndex(o => new { o.TenantId, o.CandidateId });

        builder.HasOne(o => o.Candidate)
            .WithMany()
            .HasForeignKey(o => o.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
