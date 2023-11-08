using Avalonia.Media;

namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageSectionItemDesignViewModel : NewTabPageSectionItemViewModel
{
    public NewTabPageSectionItemDesignViewModel() : base("Item", Initializers.IImage) { }

    public NewTabPageSectionItemDesignViewModel(string name, IImage? icon) : base(name, icon) { }
}
