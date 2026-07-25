using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class InterviewPanelistConfiguration : IEntityTypeConfiguration<InterviewPanelist>
{
    public void Configure(EntityTypeBuilder<InterviewPanelist> builder)
    {
        builder.ToTable("InterviewPanelists");
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.TenantId, p.InterviewId, p.InterviewerUserId }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.InterviewerUserId });

        builder.HasOne(p => p.Interview)
            .WithMany(i => i.Panelists)
            .HasForeignKey(p => p.InterviewId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
