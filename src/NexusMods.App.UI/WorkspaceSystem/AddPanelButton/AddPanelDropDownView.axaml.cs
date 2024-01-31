using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public partial class AddPanelDropDownView : ReactiveUserControl<IAddPanelDropDownViewModel>
{
    public AddPanelDropDownView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.AddPanelIconViewModels, view => view.CreatePanelComboBox.ItemsSource)
                .DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedIndex, view => view.CreatePanelComboBox.SelectedIndex)
                .DisposeWith(disposables);
        });
    }
}
