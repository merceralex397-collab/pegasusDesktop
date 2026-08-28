using Microsoft.EntityFrameworkCore;
using Pegasus.Core.Reports;

namespace Pegasus.Infrastructure.Persistence;

internal static class AssessmentReportModelConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        var states = string.Join(
            ", ",
            Enum.GetNames<AssessmentReportGenerationState>().Select(SqlLiteral));
        var kinds = string.Join(
            ", ",
            Enum.GetNames<AssessmentReportArtifactKind>().Select(SqlLiteral));

        builder.Entity<AssessmentReportVersionEntity>(entity =>
        {
            entity.ToTable("AssessmentReportVersions", table =>
            {
                table.HasCheckConstraint(
                    "CK_AssessmentReportVersions_State",
                    $"[State] IN ({states})");
                table.HasCheckConstraint(
                    "CK_AssessmentReportVersions_Version",
                    "[Version] > 0");
                table.HasCheckConstraint(
                    "CK_AssessmentReportVersions_AttemptCount",
                    "[AttemptCount] >= 0");
            });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.AssessmentFamily).HasMaxLength(80).IsRequired();
            entity.Property(item => item.AcceptedPayloadSha256).HasMaxLength(64).IsFixedLength().IsRequired();
            entity.Property(item => item.TemplateVersion).HasMaxLength(100).IsRequired();
            entity.Property(item => item.LogicalKey).HasMaxLength(400).IsRequired();
            entity.Property(item => item.State).HasMaxLength(20).IsRequired();
            entity.Property(item => item.AcceptedPayloadJson).IsRequired();
            entity.Property(item => item.FailureReason).HasMaxLength(2000);
            entity.Property(item => item.AttemptCount).IsRequired();
            entity.Property(item => item.LeaseId).HasMaxLength(64);
            entity.HasIndex(item => new
            {
                item.CaseId,
                item.AssessmentFamily,
                item.AcceptedPayloadSha256,
                item.TemplateVersion
            }).IsUnique();
            entity.HasIndex(item => new { item.CaseId, item.Version }).IsUnique();
            entity.HasIndex(item => new { item.CaseId, item.CreatedAtUtc });
            entity.HasOne(item => item.Case)
                .WithMany()
                .HasForeignKey(item => item.CaseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Predecessor)
                .WithMany()
                .HasForeignKey(item => item.PredecessorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AssessmentReportArtifactEntity>(entity =>
        {
            entity.ToTable("AssessmentReportArtifacts", table =>
            {
                table.HasCheckConstraint(
                    "CK_AssessmentReportArtifacts_Kind",
                    $"[Kind] IN ({kinds})");
                table.HasCheckConstraint(
                    "CK_AssessmentReportArtifacts_Length",
                    "[ContentLength] >= 0");
                table.HasCheckConstraint(
                    "CK_AssessmentReportArtifacts_Pages",
                    "[PageCount] >= 0");
            });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.Kind).HasMaxLength(20).IsRequired();
            entity.Property(item => item.FileName).HasMaxLength(255).IsRequired();
            entity.Property(item => item.MediaType).HasMaxLength(128).IsRequired();
            entity.Property(item => item.Sha256).HasMaxLength(64).IsFixedLength().IsRequired();
            entity.Property(item => item.TemplateVersion).HasMaxLength(100).IsRequired();
            entity.Property(item => item.EngineVersion).HasMaxLength(100).IsRequired();
            entity.HasIndex(item => new { item.ReportVersionId, item.Kind }).IsUnique();
            entity.HasIndex(item => item.OccurrenceId).IsUnique();
            entity.HasIndex(item => item.DocumentVersionId).IsUnique();
            entity.HasOne(item => item.ReportVersion)
                .WithMany(item => item.Artifacts)
                .HasForeignKey(item => item.ReportVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DocumentOccurrenceEntity>()
                .WithMany()
                .HasForeignKey(item => item.OccurrenceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DocumentVersionEntity>()
                .WithMany()
                .HasForeignKey(item => item.DocumentVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static string SqlLiteral(string value) => $"'{value.Replace("'", "''")}'";
}
