using ReactiveUI;

namespace NexusMods.Abstractions.UI;

public interface IViewModel : IActivatableViewModel
{
    public Type ViewModelInterface { get; }
}
