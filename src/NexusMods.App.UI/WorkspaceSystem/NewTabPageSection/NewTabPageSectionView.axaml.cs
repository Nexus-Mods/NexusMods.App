using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

[UsedImplicitly]
public partial class NewTabPageSectionView : ReactiveUserControl<INewTabPageSectionViewModel>
{
    public NewTabPageSectionView()
    {
        InitializeComponent();

        this.WhenActivated(_ =>
        {
            SectionNameTextBlock.Text = ViewModel?.SectionName;
        });
    }
}

