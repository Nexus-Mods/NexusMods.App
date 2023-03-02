using ReactiveUI;

namespace NexusMods.App.UI;

public interface IViewModel : IActivatableViewModel
{
    public Type ViewModelInterface { get; }
}