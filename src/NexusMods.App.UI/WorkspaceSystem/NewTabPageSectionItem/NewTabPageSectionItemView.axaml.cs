using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public partial class NewTabPageSectionItemView : ReactiveUserControl<INewTabPageSectionItemViewModel>
{
    public NewTabPageSectionItemView()
    {
        InitializeComponent();

        this.WhenActivated(_ =>
        {
            NameTextBlock.Text = ViewModel?.Name;
        });
    }
}

