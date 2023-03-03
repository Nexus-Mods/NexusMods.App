using ReactiveUI;

namespace NexusMods.App.UI.ViewModels;

public abstract class AViewModel<TInterface> : ReactiveObject, IViewModel
where TInterface : IViewModelInterface
{
    public ViewModelActivator Activator { get; } = new();

    public Type ViewModelInterface { get; } = typeof(TInterface);
}
