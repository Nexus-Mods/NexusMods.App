using FluentAssertions;
using NexusMods.App.UI.Localization;

namespace NexusMods.UI.Tests.Localization;

public class BasicLocalizerTests
{
    [Fact]
    public void WhenLanguageChanges_NewStringIsReturnedInCodeBehind()
    {
        var originalString = Localizer.Instance["MyGames"];
        Localizer.Instance.LoadLanguage("pl");
        var newString = Localizer.Instance["MyGames"];

        newString.Should().NotBe(originalString);
    }
}
