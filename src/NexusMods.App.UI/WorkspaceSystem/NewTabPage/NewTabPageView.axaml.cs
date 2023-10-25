using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

[UsedImplicitly]
public partial class NewTabPageView : ReactiveUserControl<INewTabPageViewModel>
{
    public NewTabPageView()
    {
        InitializeComponent();

        this.WhenActivated(_ =>
        {
            Sections.ItemsSource = ViewModel?.SectionViewModels;
        });
    }
}
