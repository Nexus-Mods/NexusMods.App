using Avalonia.Media;

namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageSectionItemViewModel : AViewModel<INewTabPageSectionItemViewModel>, INewTabPageSectionItemViewModel
{
    public string Name { get; }

    public IImage? Icon { get; }

    public NewTabPageSectionItemViewModel(string name, IImage? icon)
    {
        Name = name;
        Icon = icon;
    }
}
