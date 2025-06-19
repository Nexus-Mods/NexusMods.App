using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Dialog.Enums;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public interface IDialogViewModel : IViewModelInterface
{
    public DialogButtonDefinition[] ButtonDefinitions { get; }
    public R3.ReactiveCommand<ButtonDefinitionId, ButtonDefinitionId> ButtonPressCommand { get; }
    public string WindowTitle { get; }
    public double WindowWidth { get; }
}

public interface IDialogViewModel<TResult> : IDialogViewModel
{
    public TResult Result { get; set; }
}
