using Pegasus.Contracts.Vocabulary;
using Pegasus.Core.ImageIntake;
using Pegasus.Core.Intake;
using Pegasus.Web.Pages.Intake;

namespace Pegasus.IntegrationTests;

public sealed class OperatorVocabularyTests
{
    [Theory]
    [InlineData("CaseCreated", "Ready for case allocation")]
    [InlineData("NeedsSorting", "Unidentified")]
    [InlineData("BlockedIntake", "Blocked")]
    [InlineData("Unsupported", "Unsupported")]
    [InlineData("OcrRequired", "Needs text extraction")]
    [InlineData("TechnicalFailure", "Failed")]
    [InlineData("ImageIntakeRegistered", "Vehicle images registered")]
    public void IntakeDecisionLabelsUseTheBindingVocabulary(string decision, string expected)
    {
        Assert.Equal(expected, OperatorVocabulary.IntakeDecisionLabel(decision));
    }

    [Fact]
    public void WebIntakeAdapterUsesTheSameDecisionOwner()
    {
        foreach (var decision in Enum.GetValues<IntakeDecision>())
        {
            Assert.Equal(
                OperatorVocabulary.IntakeDecisionLabel(decision.ToString()),
                DetailsModel.DecisionLabel(decision));
        }
    }

    [Theory]
    [InlineData("Suggested", "AB12 CDE", "93 %", "Suggested AB12 CDE (93 % confidence)")]
    [InlineData("NoReadableResult", "", "0 %", "No readable registration")]
    [InlineData("TechnicalFailure", "", "0 %", "Technical failure")]
    [InlineData("Unavailable", "", "0 %", "Recognition unavailable")]
    public void RecognitionOutcomeLabelsHaveOneOwner(
        string outcome,
        string suggestedRegistration,
        string confidence,
        string expected)
    {
        Assert.Equal(
            expected,
            OperatorVocabulary.VrmRecognitionOutcomeLabel(
                outcome,
                suggestedRegistration,
                confidence));
    }
}
