using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class RequisitionApprovalConfiguration : IEntityTypeConfiguration<RequisitionApproval>
{
    public void Configure(EntityTypeBuilder<RequisitionApproval> builder)
    {
        builder.ToTable("RequisitionApprovals");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Decision).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Comment).HasMaxLength(1000);

        builder.HasIndex(a => new { a.TenantId, a.JobRequisitionId });

        builder.HasOne(a => a.JobRequisition)
            .WithMany(r => r.Approvals)
            .HasForeignKey(a => a.JobRequisitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
