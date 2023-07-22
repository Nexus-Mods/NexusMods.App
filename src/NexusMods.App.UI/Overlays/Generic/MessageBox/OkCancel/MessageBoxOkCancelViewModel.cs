using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.Generic.MessageBox.OkCancel;

public class MessageBoxOkCancelViewModel : AViewModel<IMessageBoxOkCancelViewModel>, IMessageBoxOkCancelViewModel
{
    [Reactive] 
    public bool IsActive { get; set; } = true;
    
    [Reactive]
    public bool DialogResult { get; set; }
}
