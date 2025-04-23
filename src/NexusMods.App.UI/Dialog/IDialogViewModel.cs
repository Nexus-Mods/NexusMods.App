using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Dialog.Enums;

namespace NexusMods.App.UI.Dialog;

public interface IDialogViewModel<TReturn> : IViewModelInterface
{    
    public TReturn? Return { get; set; }
    void SetView(IDialogView<TReturn> view);
    public string WindowTitle { get; }
    public double WindowMaxWidth { get; }
}
