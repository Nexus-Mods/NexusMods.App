using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Dialog;

public interface IDialogStandardContentViewModel : IViewModelInterface
{
    string Text { get; }
    string Heading { get; }
    IconValue? Icon { get; }
    IMarkdownRendererViewModel? MarkdownRenderer { get; }
    string InputText { get; set; }
    string InputLabel { get; set; }
    string InputWatermark { get; set; }
    string BottomText { get; }
    public R3.ReactiveCommand ClearInputCommand { get; set; }
}
