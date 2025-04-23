using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.Icons;
using TransparentValueObjects;

namespace NexusMods.App.UI.Dialog;

/// <summary>
/// Represents a unique identifier for a button definition in a message box.
/// </summary>
[ValueObject<string>]
public readonly partial struct ButtonDefinitionId { }

/// <summary>
/// Defines the properties of a button used in a message box.
/// </summary>
/// <param name="Text">The display text of the button.</param>
/// <param name="Id">The unique identifier for the button.</param>
/// <param name="LeftIcon">An optional icon displayed to the left of the button text.</param>
/// <param name="RightIcon">An optional icon displayed to the right of the button text.</param>
/// <param name="ButtonRole">The role of the button, indicating its purpose or behavior in the dialog.</param>
public record MessageBoxButtonDefinition(
    string Text,
    ButtonDefinitionId Id,
    IconValue? LeftIcon = null,
    IconValue? RightIcon = null,
    ButtonRole ButtonRole = ButtonRole.None
);
