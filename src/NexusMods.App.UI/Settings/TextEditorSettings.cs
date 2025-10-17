using JetBrains.Annotations;
using NexusMods.Sdk.Settings;
using TextMateSharp.Grammars;

namespace NexusMods.App.UI.Settings;

public record TextEditorSettings : ISettings
{
    public ThemeName ThemeName { get; [UsedImplicitly] set; } = ThemeName.Dark;

    public double FontSize { get; [UsedImplicitly] set; } = 14;

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder;
    }
}
