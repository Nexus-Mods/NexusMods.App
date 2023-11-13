using System.Collections.ObjectModel;
using DynamicData;

namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageViewModel : AViewModel<INewTabPageViewModel>, INewTabPageViewModel
{
    private readonly SourceList<INewTabPageSectionViewModel> _sectionSource = new();
    private readonly ReadOnlyObservableCollection<INewTabPageSectionViewModel> _sections;
    public ReadOnlyObservableCollection<INewTabPageSectionViewModel> Sections => _sections;

    public NewTabPageViewModel(INewTabPageSectionViewModel[] sectionViewModels)
    {
        _sectionSource.Edit(list => list.AddRange(sectionViewModels));
        _sectionSource
            .Connect()
            .Bind(out _sections)
            .Subscribe();
    }
}
