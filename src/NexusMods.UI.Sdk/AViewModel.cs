using ReactiveUI;

namespace NexusMods.UI.Sdk;

public abstract class AViewModel<TInterface> : ReactiveObject, IViewModel
    where TInterface : class, IViewModelInterface
{
    public ViewModelActivator Activator { get; } = new();

    public Type ViewModelInterface { get; } = typeof(TInterface);
}
