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


public class DialogStandardContentDesignViewModel : AViewModel<IDialogStandardContentViewModel>, IDialogStandardContentViewModel
{
    public string Text { get; } = "Sample text for design-time view.";
    public string Heading { get; } = "Sample Heading";
    public IconValue? Icon { get; } = IconValues.Cog;
    public IMarkdownRendererViewModel? MarkdownRenderer { get; } = null;
    [Reactive] public string InputText { get; set; } = "Sample input text";
    public string InputLabel { get; set; } = "Sample Input Label";
    public string InputWatermark { get; set; } = "Enter text here...";
    public string BottomText { get; } = "Sample bottom text";
    public ReactiveCommand ClearInputCommand { get; set; } = new ReactiveCommand(
        executeAsync: (_, cancellationToken) => ValueTask.CompletedTask
    );
}
