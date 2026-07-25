using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class InterviewScorecardConfiguration : IEntityTypeConfiguration<InterviewScorecard>
{
    public void Configure(EntityTypeBuilder<InterviewScorecard> builder)
    {
        builder.ToTable("InterviewScorecards");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Recommendation).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Comments).HasColumnType("text");

        // One scorecard per interviewer per interview - a submission is create-only (no update
        // path), so a repeat POST must fail here rather than silently overwrite prior feedback.
        builder.HasIndex(s => new { s.TenantId, s.InterviewId, s.InterviewerUserId }).IsUnique();

        builder.HasOne(s => s.Interview)
            .WithMany(i => i.Scorecards)
            .HasForeignKey(s => s.InterviewId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
