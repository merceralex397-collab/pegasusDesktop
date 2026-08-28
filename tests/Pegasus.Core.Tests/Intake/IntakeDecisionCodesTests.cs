using Pegasus.Core.Intake;

namespace Pegasus.Core.Tests.Intake;

public sealed class IntakeDecisionCodesTests
{
    [Fact]
    public void EveryDecisionRoundTripsThroughTheSinglePersistedVocabulary()
    {
        var decisions = Enum.GetValues<IntakeDecision>();

        Assert.Equal(decisions.Length, IntakeDecisionCodes.All.Count);
        foreach (var decision in decisions)
        {
            var code = IntakeDecisionCodes.ToCode(decision);

            Assert.Contains(code, IntakeDecisionCodes.All);
            Assert.Equal(decision, IntakeDecisionCodes.Parse(code));
            Assert.True(IntakeDecisionCodes.TryParse(code, out var parsed));
            Assert.Equal(decision, parsed);
        }
    }

    [Fact]
    public void UnknownPersistedCodeFailsClosedForProjectionAndThrowsForCommands()
    {
        Assert.False(IntakeDecisionCodes.TryParse("future_decision", out _));
        Assert.Throws<InvalidDataException>(() => IntakeDecisionCodes.Parse("future_decision"));
    }
}
