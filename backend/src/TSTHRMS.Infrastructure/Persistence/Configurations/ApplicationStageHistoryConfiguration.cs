using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class ApplicationStageHistoryConfiguration : IEntityTypeConfiguration<ApplicationStageHistory>
{
    public void Configure(EntityTypeBuilder<ApplicationStageHistory> builder)
    {
        builder.ToTable("ApplicationStageHistories");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.FromStage).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.ToStage).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.Reason).HasMaxLength(1000);

        builder.HasIndex(h => new { h.TenantId, h.ApplicationId });

        builder.HasOne(h => h.Application)
            .WithMany(a => a.StageHistory)
            .HasForeignKey(h => h.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
