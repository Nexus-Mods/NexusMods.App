using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public interface IDialogViewModel : IViewModelInterface
{
    public R3.ReactiveCommand<ButtonDefinitionId, ButtonDefinitionId> ButtonPressCommand { get; }
    public string WindowTitle { get; }
    public DialogWindowSize DialogWindowSize { get; }
    public IViewModelInterface? ContentViewModel { get; }
    public DialogButtonDefinition[] ButtonDefinitions { get; }
    public ButtonDefinitionId Result { get; set; }
}

public interface IDialogStandardContentViewModel: IViewModelInterface
{
    string? Text { get; }
    string? Heading { get; }
    IconValue? Icon { get; }
    IMarkdownRendererViewModel? MarkdownRenderer { get; }
    string InputText { get; set; }
    string InputLabel { get; set; }
    string InputWatermark { get; set; }
    
    public R3.ReactiveCommand ClearInputCommand { get; set; }
}
