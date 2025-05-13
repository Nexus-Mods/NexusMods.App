using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Dialog.Enums;

namespace NexusMods.App.UI.Dialog;

/// <summary>
/// Provides standard button definitions for message boxes, such as "OK" and "Cancel".
/// </summary>
public static class MessageBoxStandardButtons
{
    /// <summary>
    /// Represents an "OK" button with an accept action.
    /// </summary>
    public static readonly MessageBoxButtonDefinition Ok = new(
        Text: "OK",
        Id: ButtonDefinitionId.From("ok"),
        ButtonAction: ButtonAction.Accept,
        ButtonStyling: ButtonStyling.Primary
    );

    /// <summary>
    /// Represents a "Cancel" button with a reject action.
    /// </summary>
    public static readonly MessageBoxButtonDefinition Cancel = new(
        Text: "Cancel",
        Id: ButtonDefinitionId.From("cancel"),
        ButtonAction: ButtonAction.Reject
    );
}
