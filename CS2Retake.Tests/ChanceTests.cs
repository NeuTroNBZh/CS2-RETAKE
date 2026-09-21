using CS2Retake.Rules;

namespace CS2Retake.Tests;

public class ChanceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Roll_ReturnsFalse_WhenChanceIsZeroOrNegative(double chance)
    {
        Assert.False(Chance.Roll(chance, new Random(1)));
    }

    [Theory]
    [InlineData(100)]
    [InlineData(150)]
    public void Roll_ReturnsTrue_WhenChanceIsHundredOrMore(double chance)
    {
        Assert.True(Chance.Roll(chance, new Random(1)));
    }

    [Fact]
    public void Roll_IsDeterministic_ForSameSeed()
    {
        var a = new Random(42);
        var b = new Random(42);

        var rollsA = Enumerable.Range(0, 20).Select(_ => Chance.Roll(30, a)).ToList();
        var rollsB = Enumerable.Range(0, 20).Select(_ => Chance.Roll(30, b)).ToList();

        Assert.Equal(rollsA, rollsB);
    }

    [Fact]
    public void Roll_ApproximatesPercentage_OverManyRolls()
    {
        var random = new Random(7);

        var hits = Enumerable.Range(0, 10_000).Count(_ => Chance.Roll(30, random));

        Assert.InRange(hits, 2_700, 3_300);
    }
}
