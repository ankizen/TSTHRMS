using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.CustomFields;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class CustomFieldDefinitionConfiguration : IEntityTypeConfiguration<CustomFieldDefinition>
{
    public void Configure(EntityTypeBuilder<CustomFieldDefinition> builder)
    {
        builder.ToTable("CustomFieldDefinitions");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Label).IsRequired().HasMaxLength(200);
        builder.Property(d => d.FieldType).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.OptionsJson).HasColumnType("text");

        builder.HasIndex(d => new { d.TenantId, d.Name }).IsUnique();
    }
}
