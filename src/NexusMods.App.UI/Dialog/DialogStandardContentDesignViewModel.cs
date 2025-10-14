using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI.Fody.Helpers;
using ReactiveCommand = R3.ReactiveCommand;

namespace NexusMods.App.UI.Dialog;


public class DialogStandardContentDesignViewModel : AViewModel<IDialogStandardContentViewModel>, IDialogStandardContentViewModel
{
    public string Text { get; } = "Sample text for design-time view.";
    public string Heading { get; } = "Sample Heading";
    public IconValue? Icon { get; } = IconValues.Cog;
    public IMarkdownRendererViewModel? MarkdownRenderer { get; }
    public bool ShowMarkdownCopyButton { get; } = true;
    [Reactive] public string InputText { get; set; } = "Sample input text";
    public string InputLabel { get; set; } = "Sample Input Label";
    public string InputWatermark { get; set; } = "Enter text here...";
    public string BottomText { get; } = "Sample bottom text";
    public ReactiveCommand ClearInputCommand { get; set; } = new ReactiveCommand(
        executeAsync: (_, cancellationToken) => ValueTask.CompletedTask
    );
}
