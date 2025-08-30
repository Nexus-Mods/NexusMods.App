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
    public string Text { get; }
    public string Heading { get; }
    public IconValue? Icon { get; }
    public IMarkdownRendererViewModel? MarkdownRenderer { get; }
    public bool ShowMarkdownCopyButton { get; }
    [Reactive] public string InputText { get; set; }
    public string InputLabel { get; set; }
    public string InputWatermark { get; set; }
    public string BottomText { get; }
    public ReactiveCommand ClearInputCommand { get; set; }

    public DialogStandardContentViewModel(StandardDialogParameters standardDialogParameters)
    {
        Text = standardDialogParameters.Text;
        Heading = standardDialogParameters.Heading;
        Icon = standardDialogParameters.Icon;
        MarkdownRenderer = standardDialogParameters.Markdown;
        InputLabel = standardDialogParameters.InputLabel;
        InputText = standardDialogParameters.InputText;
        InputWatermark = standardDialogParameters.InputWatermark;
        BottomText = standardDialogParameters.BottomText;
        ShowMarkdownCopyButton = standardDialogParameters.ShowMarkdownCopyButton;

        ClearInputCommand = new R3.ReactiveCommand(
            executeAsync: (_, cancellationToken) =>
            {
                InputText = string.Empty;
                return ValueTask.CompletedTask;
            }
        );
    }
}
