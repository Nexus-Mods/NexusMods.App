using NexusMods.Abstractions.GuidedInstallers;

namespace NexusMods.Games.FOMOD.UI;

internal static class Extensions
{
    public static bool UsesRadioButtons(this OptionGroupType groupType)
    {
        return groupType is OptionGroupType.ExactlyOne or OptionGroupType.AtMostOne;
    }
}
