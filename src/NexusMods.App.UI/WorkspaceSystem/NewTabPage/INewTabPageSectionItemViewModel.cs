using System.Reactive;
using Avalonia.Media;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface INewTabPageSectionItemViewModel : IViewModelInterface
{
    public string Name { get; }

    public IImage? Icon { get; }

    public ReactiveCommand<Unit, PageData> SelectItemCommand { get; }
}
