using FluentAssertions;
using NexusMods.App;

namespace NexusMods.CLI.Tests;

public class StartupModeTests
{
    [Theory]
    [InlineData("", true, true, false, "")]
    [InlineData("as-main", true, false, false, "")]
    [InlineData("as-main-ui", true, true, false, "")]
    [InlineData("arg1 arg2", false, false, true, "arg1 arg2")]
    [InlineData("as-main arg1 arg2", true, false, true, "arg1 arg2")]
    [InlineData("as-main-ui arg1 arg2", true, true, true, "arg1 arg2")]
    public void TestStartupModeParsing(string input, bool asMain, bool showUi, bool executeCli, string finalArgs)
    {
        var splitInput = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var mode = StartupMode.Parse(splitInput);
        mode.RunAsMain.Should().Be(asMain);
        mode.ShowUI.Should().Be(showUi);
        mode.ExecuteCli.Should().Be(executeCli);
        string.Join(' ', mode.Args).Should().Be(finalArgs);
    }
}
