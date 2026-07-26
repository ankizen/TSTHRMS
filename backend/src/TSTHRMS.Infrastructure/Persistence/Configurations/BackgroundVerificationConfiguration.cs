using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class BackgroundVerificationConfiguration : IEntityTypeConfiguration<BackgroundVerification>
{
    public void Configure(EntityTypeBuilder<BackgroundVerification> builder)
    {
        builder.ToTable("BackgroundVerifications");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.VendorReference).HasMaxLength(100);
        builder.Property(b => b.DiscrepancyNotes).HasColumnType("text");

        builder.HasIndex(b => new { b.TenantId, b.ApplicationId }).IsUnique();

        builder.HasOne(b => b.Application)
            .WithMany()
            .HasForeignKey(b => b.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
