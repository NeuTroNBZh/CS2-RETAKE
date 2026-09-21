using CS2Retake.Entities;
using CS2Retake.Rules;
using CSZoneNet.Plugin.Utils.Enums;

namespace CS2Retake.Tests;

public class RoundTypeSequenceTests
{
    private static List<RoundTypeSequenceEntity> DefaultSequence() => new()
    {
        new RoundTypeSequenceEntity(RoundTypeEnum.Pistol, 5),
        new RoundTypeSequenceEntity(RoundTypeEnum.Mid, 3),
        new RoundTypeSequenceEntity(RoundTypeEnum.FullBuy, -1),
    };

    [Theory]
    [InlineData(12, 13)]
    [InlineData(24, 25)]
    public void Build_FillsRemainingRoundsForNegativeAmount(int maxRounds, int expectedCount)
    {
        var list = RoundTypeSequence.Build(DefaultSequence(), maxRounds);

        Assert.Equal(expectedCount, list.Count);
    }

    [Fact]
    public void Build_OrdersPistolThenMidThenFullBuy()
    {
        var list = RoundTypeSequence.Build(DefaultSequence(), 12);

        Assert.All(list.Take(5), t => Assert.Equal(RoundTypeEnum.Pistol, t));
        Assert.All(list.Skip(5).Take(3), t => Assert.Equal(RoundTypeEnum.Mid, t));
        Assert.All(list.Skip(8), t => Assert.Equal(RoundTypeEnum.FullBuy, t));
    }

    [Fact]
    public void Build_WithoutNegativeEntry_ContainsExactAmounts()
    {
        var sequence = new List<RoundTypeSequenceEntity>
        {
            new(RoundTypeEnum.Pistol, 2),
            new(RoundTypeEnum.FullBuy, 3),
        };

        var list = RoundTypeSequence.Build(sequence, 24);

        Assert.Equal(new[]
        {
            RoundTypeEnum.Pistol, RoundTypeEnum.Pistol,
            RoundTypeEnum.FullBuy, RoundTypeEnum.FullBuy, RoundTypeEnum.FullBuy,
        }, list);
    }

    [Fact]
    public void Build_NegativeEntryAfterFullList_AddsNothing()
    {
        var sequence = new List<RoundTypeSequenceEntity>
        {
            new(RoundTypeEnum.Pistol, 20),
            new(RoundTypeEnum.FullBuy, -1),
        };

        var list = RoundTypeSequence.Build(sequence, 12);

        Assert.Equal(20, list.Count);
        Assert.DoesNotContain(RoundTypeEnum.FullBuy, list);
    }

    [Fact]
    public void Build_EmptySequence_ReturnsEmptyList()
    {
        Assert.Empty(RoundTypeSequence.Build(new List<RoundTypeSequenceEntity>(), 12));
    }

    [Theory]
    [InlineData(0, RoundTypeEnum.Pistol)]
    [InlineData(4, RoundTypeEnum.Pistol)]
    [InlineData(5, RoundTypeEnum.Mid)]
    [InlineData(8, RoundTypeEnum.FullBuy)]
    [InlineData(12, RoundTypeEnum.FullBuy)]
    public void TryGetAt_ReturnsTypeForPlayedRounds(int played, RoundTypeEnum expected)
    {
        var list = RoundTypeSequence.Build(DefaultSequence(), 12);

        Assert.Equal(expected, RoundTypeSequence.TryGetAt(list, played, 12));
    }

    [Fact]
    public void TryGetAt_ReturnsNull_WhenPlayedExceedsMaxRounds()
    {
        var list = RoundTypeSequence.Build(DefaultSequence(), 12);

        Assert.Null(RoundTypeSequence.TryGetAt(list, 13, 12));
    }

    [Fact]
    public void TryGetAt_ReturnsNull_WhenPlayedBeyondList()
    {
        var list = new List<RoundTypeEnum> { RoundTypeEnum.Pistol };

        Assert.Null(RoundTypeSequence.TryGetAt(list, 1, 12));
    }
}
