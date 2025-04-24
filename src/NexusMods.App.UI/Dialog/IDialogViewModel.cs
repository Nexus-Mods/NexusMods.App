using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Dialog.Enums;

namespace NexusMods.App.UI.Dialog;

public interface IDialogViewModel<TResult> : IViewModelInterface
{
    public IDialogView<ButtonDefinitionId>? View { get; set; }
    public TResult? Result { get; set; }
    void SetView(IDialogView<TResult> view);
    public string WindowTitle { get; }
    public double WindowMaxWidth { get; }
    public bool ShowWindowTitlebar { get; }
}
