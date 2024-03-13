using JetBrains.Annotations;
using MouseButton = Avalonia.Input.MouseButton;
using KeyboardKey = Avalonia.Input.Key;
using KeyboardModifiers = Avalonia.Input.KeyModifiers;

namespace NexusMods.App.UI.WorkspaceSystem;

// TODO: Show in Settings (https://github.com/Nexus-Mods/NexusMods.App/issues/396)
// Related: https://github.com/Nexus-Mods/NexusMods.App/issues/946

[PublicAPI]
public class OpenPageBehaviorSettings : Dictionary<NavigationInput, OpenPageBehaviorType>
{
    public static readonly OpenPageBehaviorSettings DefaultWithData = new()
    {
        // Primary Inputs
        { new NavigationInput(MouseButton.Left, KeyboardModifiers.None), OpenPageBehaviorType.NewTab },
        { new NavigationInput(KeyboardKey.Enter, KeyboardModifiers.None), OpenPageBehaviorType.NewTab },

        // Others
        { new NavigationInput(MouseButton.Middle, KeyboardModifiers.None), OpenPageBehaviorType.NewTab },

        { new NavigationInput(MouseButton.Left, KeyboardModifiers.Control), OpenPageBehaviorType.ReplaceTab },
        { new NavigationInput(MouseButton.Left, KeyboardModifiers.Shift), OpenPageBehaviorType.NewTab },
    };

    public static readonly OpenPageBehaviorSettings DefaultWithoutData = new()
    {
        // Primary Inputs
        { new NavigationInput(MouseButton.Left, KeyboardModifiers.None), OpenPageBehaviorType.ReplaceTab },
        { new NavigationInput(KeyboardKey.Enter, KeyboardModifiers.None), OpenPageBehaviorType.ReplaceTab },

        // Others
        { new NavigationInput(MouseButton.Middle, KeyboardModifiers.None), OpenPageBehaviorType.NewTab },

        { new NavigationInput(MouseButton.Left, KeyboardModifiers.Control), OpenPageBehaviorType.ReplaceTab },
        { new NavigationInput(MouseButton.Left, KeyboardModifiers.Shift), OpenPageBehaviorType.NewTab },
    };
}
