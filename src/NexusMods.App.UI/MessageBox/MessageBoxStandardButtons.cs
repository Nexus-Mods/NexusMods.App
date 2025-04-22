using NexusMods.App.UI.Controls;
using NexusMods.App.UI.MessageBox.Enums;

namespace NexusMods.App.UI.MessageBox;

/// <summary>
/// Provides standard button definitions for message boxes, such as "OK" and "Cancel".
/// </summary>
public static class MessageBoxStandardButtons
{
    /// <summary>
    /// Represents an "OK" button with an accept role.
    /// </summary>
    public static readonly MessageBoxButtonDefinition Ok = new(
        "OK",
        ButtonDefinitionId.From("ok"),
        null,
        null,
        ButtonRole.AcceptRole
    );

    /// <summary>
    /// Represents a "Cancel" button with a reject role.
    /// </summary>
    public static readonly MessageBoxButtonDefinition Cancel = new(
        "Cancel",
        ButtonDefinitionId.From("cancel"),
        null,
        null,
        ButtonRole.RejectRole
    );
}
