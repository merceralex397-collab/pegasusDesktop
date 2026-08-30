using Pegasus.Core.Intake;
using Pegasus.Web.Presentation;

namespace Pegasus.IntegrationTests;

/// <summary>
/// MAIL-008: every settled mail category renders through the one operator
/// label map — no kebab-case registry key, no raw enum name, no fallback.
/// </summary>
public sealed class MailClassificationLabelTests
{
    [Fact]
    public void EveryReceivedFamilyAndSubtypeHasAnOperatorLabel()
    {
        foreach (var family in Enum.GetValues<ReceivedMailFamily>())
        {
            AssertOperatorWorded(OperatorLabels.MailClassification(MailCategory.Received(family)));
            foreach (var subtype in MailTaxonomy.ConfirmedReceivedSubtypes[family])
            {
                AssertOperatorWorded(
                    OperatorLabels.MailClassification(MailCategory.Received(family, subtype)));
            }
        }
    }

    [Fact]
    public void EverySentFamilyHasAnOperatorLabel()
    {
        foreach (var family in Enum.GetValues<SentMailFamily>())
        {
            AssertOperatorWorded(OperatorLabels.MailClassification(MailCategory.Sent(family)));
        }
    }

    [Fact]
    public void AnOtherCategoryRendersTheOperatorsOwnName()
    {
        var category = MailCategory.Other(
            MailDirection.Received,
            "Salvage circulars",
            "Circulated salvage lists fit no settled family.");

        Assert.Equal("Salvage circulars", OperatorLabels.MailClassification(category));
    }

    [Fact]
    public void EverySettledFamilyAndSubtypeKeepsItsExactOperatorWords()
    {
        Assert.Equal("General", OperatorLabels.MailClassification(MailCategory.Received(ReceivedMailFamily.General)));
        Assert.Equal("Billing", OperatorLabels.MailClassification(MailCategory.Received(ReceivedMailFamily.Billing)));
        Assert.Equal("New instruction", OperatorLabels.MailClassification(MailCategory.Received(ReceivedMailFamily.NewInstructionReceived)));
        Assert.Equal("Not client related", OperatorLabels.MailClassification(MailCategory.Received(ReceivedMailFamily.NonClientRelated)));
        Assert.Equal("In-progress case", OperatorLabels.MailClassification(MailCategory.Received(ReceivedMailFamily.InProgressCases)));
        Assert.Equal("Post-report", OperatorLabels.MailClassification(MailCategory.Received(ReceivedMailFamily.PostReportEmails)));
        Assert.Equal("Pre-instruction", OperatorLabels.MailClassification(MailCategory.Received(ReceivedMailFamily.PreInstructionEmails)));
        Assert.Equal("Internal CC", OperatorLabels.MailClassification(MailCategory.Received(ReceivedMailFamily.InternalCc)));
        Assert.Equal("Sent · Report sent", OperatorLabels.MailClassification(MailCategory.Sent(SentMailFamily.ReportSent)));
        Assert.Equal("Sent · Case rejected", OperatorLabels.MailClassification(MailCategory.Sent(SentMailFamily.CaseRejected)));
        Assert.Equal("Sent · Query sent", OperatorLabels.MailClassification(MailCategory.Sent(SentMailFamily.QuerySent)));
        Assert.Equal("Sent · Additional image request", OperatorLabels.MailClassification(MailCategory.Sent(SentMailFamily.AdditionalImageRequest)));
        Assert.Equal(
            "New instruction · Inspection",
            OperatorLabels.MailClassification(MailCategory.Received(
                ReceivedMailFamily.NewInstructionReceived,
                "inspection")));
    }

    [Fact]
    public void RegistryNamesStillRoundTrip()
    {
        foreach (var family in Enum.GetValues<ReceivedMailFamily>())
        {
            Assert.Equal(family, MailTaxonomy.ParseReceivedFamily(MailTaxonomy.CategoryName(family)));
        }

        foreach (var family in Enum.GetValues<SentMailFamily>())
        {
            Assert.Equal(family, MailTaxonomy.ParseSentFamily(MailTaxonomy.CategoryName(family)));
        }
    }

    private static void AssertOperatorWorded(string label)
    {
        Assert.False(string.IsNullOrWhiteSpace(label));
        // No registry key reaches the operator: no kebab-case, no slash-joined
        // family/subtype, and the label starts with a capital.
        Assert.DoesNotContain("-received", label, StringComparison.Ordinal);
        Assert.DoesNotContain("-cases", label, StringComparison.Ordinal);
        Assert.DoesNotContain("-emails", label, StringComparison.Ordinal);
        Assert.DoesNotContain("/", label, StringComparison.Ordinal);
        Assert.True(char.IsUpper(label[0]), $"'{label}' does not start with a capital.");
    }
}
