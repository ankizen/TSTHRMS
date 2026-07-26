using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class PreboardingChecklistItemConfiguration : IEntityTypeConfiguration<PreboardingChecklistItem>
{
    public void Configure(EntityTypeBuilder<PreboardingChecklistItem> builder)
    {
        builder.ToTable("PreboardingChecklistItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.TaskType).HasConversion<string>().HasMaxLength(40);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.BankAccountNumber).HasMaxLength(50);
        builder.Property(i => i.BankIfscCode).HasMaxLength(20);

        builder.HasIndex(i => new { i.TenantId, i.ApplicationId, i.TaskType }).IsUnique();

        builder.HasOne(i => i.Application)
            .WithMany()
            .HasForeignKey(i => i.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Document)
            .WithMany()
            .HasForeignKey(i => i.DocumentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
