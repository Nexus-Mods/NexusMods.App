using NexusMods.App.UI.Dialog.Enums;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Dialog;

namespace NexusMods.App.UI.Dialog;

public interface IDialogViewModel : IViewModelInterface
{
    public R3.ReactiveCommand<ButtonDefinitionId, ButtonDefinitionId> ButtonPressCommand { get; }
    public string WindowTitle { get; }
    public DialogWindowSize DialogWindowSize { get; }
    public IViewModelInterface? ContentViewModel { get; }
    public DialogButtonDefinition[] ButtonDefinitions { get; }
    public bool ShowChrome { get; set; }
    public StandardDialogResult Result { get; set; }
}
