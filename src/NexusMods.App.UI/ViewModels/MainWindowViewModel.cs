using NexusMods.App.UI.Controls.Spine;
using ReactiveUI;

namespace NexusMods.App.UI.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    public MainWindowViewModel(SpineViewModel spineViewModel)
    {
        Spine = spineViewModel;
    }
    
    public SpineViewModel Spine { get; }
    public string Greeting => "Welcome to Avalonia!";
}
