namespace NexusMods.App.UI.Dialog;

public record StandardDialogResult
{
    public ButtonDefinitionId ButtonId { get; set; } = ButtonDefinitionId.From("none");
    public string InputText { get; set; } = string.Empty;
}
