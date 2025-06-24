using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public interface IDialogViewModel : IViewModelInterface, IDialogBaseModel
{
    public R3.ReactiveCommand<ButtonDefinitionId, ButtonDefinitionId> ButtonPressCommand { get; }
    public string WindowTitle { get; }
    public double WindowWidth { get; }
}

public interface IDialogViewModel<TResult> : IDialogViewModel
{
    public TResult Result { get; set; }
}
