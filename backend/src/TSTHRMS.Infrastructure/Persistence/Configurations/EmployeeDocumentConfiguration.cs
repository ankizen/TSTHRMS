using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Documents;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.ToTable("EmployeeDocuments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Category).HasConversion<string>().HasMaxLength(30);
        builder.Property(d => d.Notes).HasMaxLength(500);

        builder.HasIndex(d => new { d.TenantId, d.EmployeeId });

        builder.HasOne(d => d.Employee)
            .WithMany()
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Document)
            .WithMany()
            .HasForeignKey(d => d.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
