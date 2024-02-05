using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

[UsedImplicitly]
public partial class AddPanelDropDownView : ReactiveUserControl<IAddPanelDropDownViewModel>
{
    public AddPanelDropDownView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.AddPanelButtonViewModel, view => view.CreatePanelComboBox.ItemsSource)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.SelectedItem, view => view.CreatePanelComboBox.SelectedItem)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.SelectedIndex, view => view.CreatePanelComboBox.SelectedIndex)
                .DisposeWith(disposables);
        });
    }
}
