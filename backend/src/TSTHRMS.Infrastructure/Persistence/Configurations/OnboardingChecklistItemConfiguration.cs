using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class OnboardingChecklistItemConfiguration : IEntityTypeConfiguration<OnboardingChecklistItem>
{
    public void Configure(EntityTypeBuilder<OnboardingChecklistItem> builder)
    {
        builder.ToTable("OnboardingChecklistItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.TaskType).HasConversion<string>().HasMaxLength(30);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(i => new { i.TenantId, i.EmployeeId, i.TaskType }).IsUnique();

        // No navigation/FK to Employees - Domain.Recruitment doesn't reference Domain.Employees.
        // Cascade-on-delete isn't available without one, but Employee rows are never hard-deleted
        // (Section 15's soft-delete-only rule), so an orphaned row here can't actually occur.
    }
}
