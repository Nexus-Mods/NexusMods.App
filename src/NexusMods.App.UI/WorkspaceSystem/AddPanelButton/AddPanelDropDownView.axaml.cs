using System.Reactive.Disposables;
using System.Reactive.Linq;
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
            this.OneWayBind(ViewModel, vm => vm.AddPanelButtonViewModels, view => view.CreatePanelComboBox.ItemsSource)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.AddPanelButtonViewModels.Count)
                .Select(count => count > 0)
                .BindToView(this, view => view.CreatePanelComboBox.IsEnabled)
                .DisposeWith(disposables);

            // close the dropdown when one of the items is pressed
            this.WhenAnyValue(view => view.ViewModel!.SelectedIndex)
                .Subscribe(_ => { CreatePanelComboBox.IsDropDownOpen = false;})
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.SelectedItem, view => view.CreatePanelComboBox.SelectedItem)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.SelectedIndex, view => view.CreatePanelComboBox.SelectedIndex)
                .DisposeWith(disposables);
        });
    }
}
