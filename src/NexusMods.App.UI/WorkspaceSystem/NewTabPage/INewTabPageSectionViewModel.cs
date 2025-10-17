using System.Collections.ObjectModel;
using NexusMods.UI.Sdk;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface INewTabPageSectionViewModel : IViewModelInterface
{
    public string SectionName { get; }

    public ReadOnlyObservableCollection<INewTabPageSectionItemViewModel> Items { get; }
}
