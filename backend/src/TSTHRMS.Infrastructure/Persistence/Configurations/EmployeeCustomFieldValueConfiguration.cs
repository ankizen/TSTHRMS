using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.CustomFields;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class EmployeeCustomFieldValueConfiguration : IEntityTypeConfiguration<EmployeeCustomFieldValue>
{
    public void Configure(EntityTypeBuilder<EmployeeCustomFieldValue> builder)
    {
        builder.ToTable("EmployeeCustomFieldValues");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Value).HasMaxLength(500);

        builder.HasIndex(v => new { v.TenantId, v.EmployeeId, v.CustomFieldDefinitionId }).IsUnique();

        builder.HasOne(v => v.Employee)
            .WithMany()
            .HasForeignKey(v => v.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.CustomFieldDefinition)
            .WithMany()
            .HasForeignKey(v => v.CustomFieldDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
