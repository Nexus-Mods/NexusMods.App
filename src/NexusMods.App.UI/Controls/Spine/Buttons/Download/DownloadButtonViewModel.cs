using System.Windows.Input;
using NexusMods.Abstractions.Values;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Download;

public class DownloadButtonViewModel : AViewModel<IDownloadButtonViewModel>, IDownloadButtonViewModel
{
    [Reactive]
    public float Number { get; set; } = 4.2f;
    
    [Reactive]
    public string Units { get; set; } = "MB/s";
    
    [Reactive]
    public Percent? Progress { get; set; }
    
    [Reactive]
    public ICommand Click { get; set; } = Initializers.ICommand;
    
    [Reactive]
    public bool IsActive { get; set; }
}
