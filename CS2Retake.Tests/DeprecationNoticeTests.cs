using CS2Retake.Rules;

namespace CS2Retake.Tests;

public class DeprecationNoticeTests
{
    [Fact]
    public void Message_PointsToRetakeV4()
    {
        Assert.Contains("https://github.com/NeuTroNBZh/CS2-RetakeV4", DeprecationNotice.Message);
    }

    [Fact]
    public void Message_SaysThePluginIsDeprecated()
    {
        Assert.Contains("deprecated", DeprecationNotice.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Message_IsASingleLine()
    {
        Assert.DoesNotContain('\n', DeprecationNotice.Message);
    }
}
