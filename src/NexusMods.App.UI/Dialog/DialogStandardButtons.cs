using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Dialog.Enums;

namespace NexusMods.App.UI.Dialog;

/// <summary>
/// Provides standard button definitions for message boxes, such as "OK" and "Cancel".
/// </summary>
public static class DialogStandardButtons
{
    /// <summary>
    /// Represents an "OK" button with an accept action.
    /// </summary>
    public static readonly DialogButtonDefinition Ok = new(
        Text: "OK",
        Id: ButtonDefinitionId.From("Ok"),
        ButtonAction: ButtonAction.Accept,
        ButtonStyling: ButtonStyling.Primary
    );
    
    /// <summary>
    /// Represents an "Yes" button with an accept action.
    /// </summary>
    public static readonly DialogButtonDefinition Yes = new(
        Text: "Yes",
        Id: ButtonDefinitionId.From("Yes"),
        ButtonAction: ButtonAction.Accept,
        ButtonStyling: ButtonStyling.Primary
    );
    
    /// <summary>
    /// Represents a "No" button with an accept action.
    /// </summary>
    public static readonly DialogButtonDefinition No = new(
        Text: "No",
        Id: ButtonDefinitionId.From("No"),
        ButtonAction: ButtonAction.Reject
    );
    
    /// <summary>
    /// Represents a "Cancel" button with a reject action.
    /// </summary>
    public static readonly DialogButtonDefinition Cancel = new(
        Text: "Cancel",
        Id: ButtonDefinitionId.From("Cancel"),
        ButtonAction: ButtonAction.Reject
    );
}
