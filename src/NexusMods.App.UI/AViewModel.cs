using ReactiveUI;

namespace NexusMods.App.UI;

public abstract class AViewModel<TInterface> : ReactiveObject, IViewModel
{
    public ViewModelActivator Activator { get; } = new();

    public Type ViewModelInterface { get; } = typeof(TInterface);
}
