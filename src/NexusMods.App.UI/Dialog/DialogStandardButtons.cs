using NexusMods.UI.Sdk.Dialog;
using NexusMods.UI.Sdk.Dialog.Enums;

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
        Id: ButtonDefinitionId.Accept,
        ButtonAction: ButtonAction.Accept,
        ButtonStyling: ButtonStyling.Primary
    );
    
    /// <summary>
    /// Represents a "Yes" button with an accept action.
    /// </summary>
    public static readonly DialogButtonDefinition Yes = new(
        Text: "Yes",
        Id: ButtonDefinitionId.Accept,
        ButtonAction: ButtonAction.Accept,
        ButtonStyling: ButtonStyling.Default
    );
    
    /// <summary>
    /// Represents a "No" button with a reject action.
    /// </summary>
    public static readonly DialogButtonDefinition No = new(
        Text: "No",
        Id: ButtonDefinitionId.No,
        ButtonAction: ButtonAction.Reject
    );
    
    /// <summary>
    /// Represents a "Cancel" button with a reject action.
    /// </summary>
    public static readonly DialogButtonDefinition Cancel = new(
        Text: "Cancel",
        Id: ButtonDefinitionId.Cancel,
        ButtonAction: ButtonAction.Reject
    );

    public static readonly DialogButtonDefinition Close = new(
        Text: "Close",
        Id: ButtonDefinitionId.Close,
        ButtonAction.Reject
    );
}
