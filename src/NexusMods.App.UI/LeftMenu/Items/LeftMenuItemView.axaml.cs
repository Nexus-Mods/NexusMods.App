using Avalonia.ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public partial class LeftMenuItemView : ReactiveUserControl<INewLeftMenuItemViewModel>
{
    public LeftMenuItemView()
    {
        InitializeComponent();
    }
}

