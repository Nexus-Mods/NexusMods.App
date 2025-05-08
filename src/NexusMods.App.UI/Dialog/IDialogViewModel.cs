using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Dialog.Enums;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public interface IDialogViewModel<TResult> : IViewModelInterface
{
    public IDialogView<ButtonDefinitionId>? View { get; set; }
    public TResult? Result { get; set; }
    void SetView(IDialogView<TResult> view);
    
    void CloseWindow(ButtonDefinitionId id);
    public string WindowTitle { get; }
    public double WindowMaxWidth { get; }
    public bool ShowWindowTitlebar { get; }
}
