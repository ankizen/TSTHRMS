using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Infrastructure.Persistence;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class TenantSequenceConfiguration : IEntityTypeConfiguration<TenantSequence>
{
    public void Configure(EntityTypeBuilder<TenantSequence> builder)
    {
        builder.ToTable("TenantSequences");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(s => new { s.TenantId, s.Name }).IsUnique();
    }
}
