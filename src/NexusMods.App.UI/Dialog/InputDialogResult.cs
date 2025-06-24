namespace NexusMods.App.UI.Dialog;

public record struct InputDialogResult
{
    public ButtonDefinitionId ButtonId { get; set; }
    public string InputText { get; set; }
}
