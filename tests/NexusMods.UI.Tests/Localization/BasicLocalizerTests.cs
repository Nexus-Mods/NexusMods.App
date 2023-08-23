using FluentAssertions;
using NexusMods.App.UI.Localization;
using NexusMods.App.UI.Resources;

namespace NexusMods.UI.Tests.Localization;

public class BasicLocalizerTests
{
    [Fact]
    [Trait("FlakeyTest", "True")]
    public void WhenLanguageChanges_NewStringIsReturnedInCodeBehind()
    {
        var originalString = Localizer.Instance["MyGames"];
        Localizer.Instance.LoadLanguage("pl");
        var newString = Localizer.Instance["MyGames"];

        newString.Should().NotBe(originalString);
    }

    [Fact]
    [Trait("FlakeyTest", "True")]
    public void WhenLanguageChanges_CallbackIsFired()
    {
        // Verifies that the LocalizedStringUpdater is called back to from Localizer
        var newString = "";
        using var localizable = new LocalizedStringUpdater(() => newString = Language.MyGames);
        var original = Language.MyGames;

        // The action is executed upon initialization, so this should be equal.
        original.Should().Be(newString);

        // Now change the locale, verify the string was changed.
        Localizer.Instance.LoadLanguage("pl");
        original.Should().NotBe(newString);
    }
}
