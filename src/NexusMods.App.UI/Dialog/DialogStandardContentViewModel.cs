using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Threading;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveCommand = R3.ReactiveCommand;

namespace NexusMods.App.UI.Dialog;

public class DialogStandardContentViewModel : AViewModel<IDialogStandardContentViewModel>, IDialogStandardContentViewModel
{
    public string? Text { get; }
    public string? Heading { get; }
    public IconValue? Icon { get; }
    public IMarkdownRendererViewModel? MarkdownRenderer { get; }
    [Reactive] public string InputText { get; set; }
    public string InputLabel { get; set; }
    public string InputWatermark { get; set; }

    public ReactiveCommand ClearInputCommand { get; set; }

    public DialogStandardContentViewModel(string text)
    {
        Text = text;
        Heading = "Dialog Heading";
        Icon = IconValues.AccountCog;
        MarkdownRenderer = null; // Set to null or provide an instance if needed
        InputText = string.Empty;
        InputLabel = "Input Label";
        InputWatermark = "Enter text here...";

        ClearInputCommand = new R3.ReactiveCommand(
            executeAsync: (_, cancellationToken) =>
            {
                InputText = string.Empty;
                return ValueTask.CompletedTask;
            }
        );
    }
}
