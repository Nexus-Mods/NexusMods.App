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

public struct DialogParameters
{
    public string Text { get; set; }
    public string Heading { get; set; }
    public IconValue? Icon { get; set; }
    public IMarkdownRendererViewModel? Markdown { get; set; }
    public string InputLabel { get; set; }
    public string InputWatermark { get; set; }
    public string InputText { get; set; }
    public string BottomText { get; set; }
}

public class DialogStandardContentViewModel : AViewModel<IDialogStandardContentViewModel>, IDialogStandardContentViewModel
{
    public string Text { get; }
    public string Heading { get; }
    public IconValue? Icon { get; }
    public IMarkdownRendererViewModel? MarkdownRenderer { get; }
    [Reactive] public string InputText { get; set; }
    public string InputLabel { get; set; }
    public string InputWatermark { get; set; }
    public string BottomText { get; }
    public ReactiveCommand ClearInputCommand { get; set; }

    public DialogStandardContentViewModel(DialogParameters dialogParameters)
    {
        Text = dialogParameters.Text;
        Heading = dialogParameters.Heading;
        Icon = dialogParameters.Icon;
        MarkdownRenderer = dialogParameters.Markdown;
        InputLabel = dialogParameters.InputLabel;
        InputText = dialogParameters.InputText;
        InputWatermark = dialogParameters.InputWatermark;
        BottomText = dialogParameters.BottomText;

        ClearInputCommand = new R3.ReactiveCommand(
            executeAsync: (_, cancellationToken) =>
            {
                InputText = string.Empty;
                return ValueTask.CompletedTask;
            }
        );
    }
}
