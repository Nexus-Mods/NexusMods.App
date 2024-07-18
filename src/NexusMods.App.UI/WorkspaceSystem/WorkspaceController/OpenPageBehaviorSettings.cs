using JetBrains.Annotations;
using MouseButton = Avalonia.Input.MouseButton;
using KeyboardKey = Avalonia.Input.Key;
using KeyboardModifiers = Avalonia.Input.KeyModifiers;

namespace NexusMods.App.UI.WorkspaceSystem;

// TODO: https://github.com/Nexus-Mods/NexusMods.App/issues/1058
// TODO: https://github.com/Nexus-Mods/NexusMods.App/issues/946

[PublicAPI]
public class OpenPageBehaviorSettings : Dictionary<NavigationInput, OpenPageBehaviorType>
{
    public static readonly OpenPageBehaviorSettings Default = new()
    {
        // Primary Inputs
        { new NavigationInput(MouseButton.Left, KeyboardModifiers.None), OpenPageBehaviorType.ReplaceTab },
        { new NavigationInput(KeyboardKey.Enter, KeyboardModifiers.None), OpenPageBehaviorType.ReplaceTab },

        // Others
        { new NavigationInput(MouseButton.Middle, KeyboardModifiers.None), OpenPageBehaviorType.NewTab },
    };
}
