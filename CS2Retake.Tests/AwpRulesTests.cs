using CS2Retake.Rules;

namespace CS2Retake.Tests;

public class AwpRulesTests
{
    private sealed record Volunteer(string Name, int AwpChance);

    [Theory]
    [InlineData(0, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(10, true)]
    public void IsLobbyEligible_RequiresFiveActivePlayers(int activePlayers, bool expected)
    {
        Assert.Equal(expected, AwpRules.IsLobbyEligible(activePlayers));
    }

    [Fact]
    public void MinActivePlayers_IsFive()
    {
        Assert.Equal(5, AwpRules.MinActivePlayers);
    }

    [Fact]
    public void PickRecipient_ReturnsNull_WhenNoVolunteers()
    {
        var result = AwpRules.PickRecipient(Array.Empty<Volunteer>(), v => v.AwpChance, new Random(1));

        Assert.Null(result);
    }

    [Fact]
    public void PickRecipient_ReturnsNull_WhenPickedVolunteerHasZeroChance()
    {
        var volunteers = new[] { new Volunteer("a", 0) };

        var result = AwpRules.PickRecipient(volunteers, v => v.AwpChance, new Random(1));

        Assert.Null(result);
    }

    [Fact]
    public void PickRecipient_ReturnsVolunteer_WhenChanceIsHundred()
    {
        var volunteers = new[] { new Volunteer("a", 100) };

        var result = AwpRules.PickRecipient(volunteers, v => v.AwpChance, new Random(1));

        Assert.Same(volunteers[0], result);
    }

    [Fact]
    public void PickRecipient_ReturnsOneOfTheVolunteers()
    {
        var volunteers = new[] { new Volunteer("a", 100), new Volunteer("b", 100), new Volunteer("c", 100) };

        var result = AwpRules.PickRecipient(volunteers, v => v.AwpChance, new Random(3));

        Assert.Contains(result, volunteers);
    }

    [Fact]
    public void PickRecipient_EventuallyPicksEveryVolunteer()
    {
        var volunteers = new[] { new Volunteer("a", 100), new Volunteer("b", 100), new Volunteer("c", 100) };
        var random = new Random(11);

        var picked = Enumerable.Range(0, 200)
            .Select(_ => AwpRules.PickRecipient(volunteers, v => v.AwpChance, random))
            .ToHashSet();

        Assert.Equal(volunteers.ToHashSet(), picked);
    }

    [Fact]
    public void PickRecipient_DoesNotModifyVolunteers()
    {
        var volunteers = new List<Volunteer> { new("a", 100), new("b", 100) };
        var snapshot = volunteers.ToList();

        AwpRules.PickRecipient(volunteers, v => v.AwpChance, new Random(1));

        Assert.Equal(snapshot, volunteers);
    }
}
