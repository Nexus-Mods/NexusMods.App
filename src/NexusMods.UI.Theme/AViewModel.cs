using ReactiveUI;

namespace NexusMods.App.UI.ViewModels;

public abstract class AViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();
}
