using ReactiveUI;

namespace NexusMods.UI.Sdk;

public interface IViewModel : IActivatableViewModel
{
    public Type ViewModelInterface { get; }
}
