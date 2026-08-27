using Pegasus.Core.Assessment;
using Pegasus.Core.Reports;

namespace Pegasus.Core.Tests.Reports;

public sealed class AssessmentReportGenerationTests
{
    [Fact]
    public void LogicalKeyIsDeterministicAndChangesWithAcceptedPayload()
    {
        var caseId = Guid.NewGuid();
        var snapshot = AssessmentReportRenderingTests.Snapshot(AssessmentReportOutcome.Repairable) with
        {
            CaseId = caseId
        };

        var first = AssessmentReportPayload.Key(snapshot);
        var replay = AssessmentReportPayload.Key(snapshot with { });
        var midnightReplay = AssessmentReportPayload.Key(
            snapshot with { ReportDate = snapshot.ReportDate.AddDays(1) });
        var caseEditReplay = AssessmentReportPayload.Key(
            snapshot with { AssessmentCaseVersion = snapshot.AssessmentCaseVersion + 1 });
        var correction = AssessmentReportPayload.Key(snapshot with { EngineerComments = "Corrected" });

        Assert.Equal(first, replay);
        Assert.Equal(first, midnightReplay);
        Assert.Equal(first, caseEditReplay);
        Assert.Equal(snapshot.ReportDate, AssessmentReportPayload.Deserialize(
            AssessmentReportPayload.Serialize(snapshot)).ReportDate);
        Assert.NotEqual(first.AcceptedPayloadSha256, correction.AcceptedPayloadSha256);
        Assert.Equal(caseId, first.CaseId);
        Assert.Equal("accepted-assessment", first.AssessmentFamily);
    }

    [Fact]
    public void ImportedEstimateKeepsItsAcceptedAmountsWithoutInventingARate()
    {
        var costs = ReportRepairCosts.FromAcceptedBasis(
            new RepairCalculationBasis(100m, 20m, 10m, 0m, true, 26m, 156m, "external-estimate/v1"));

        Assert.True(costs.IsImported);
        Assert.Equal(100m, costs.Labour);
        Assert.Equal(0m, costs.HourlyRate);
        Assert.Equal(26m, costs.Vat);
        Assert.Equal(156m, costs.Total);
        Assert.Equal("external-estimate/v1", costs.ImportedPolicyVersion);
    }
}
