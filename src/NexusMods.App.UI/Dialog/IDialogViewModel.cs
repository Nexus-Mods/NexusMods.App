using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Dialog.Enums;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public interface IDialogViewModel<TResult> : IViewModelInterface
{
    public TResult? Result { get; set; }
    public ReactiveCommand<TResult, TResult> CloseWindowCommand { get; }
    public string WindowTitle { get; }
    public double WindowWidth { get; }
}
