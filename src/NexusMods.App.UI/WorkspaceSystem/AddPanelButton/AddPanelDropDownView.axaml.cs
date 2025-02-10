using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI.Resources;
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

            // disable the dropdown if there are no items
            this.WhenAnyValue(view => view.ViewModel!.AddPanelButtonViewModels.Count)
                .Select(count => count > 0)
                .Subscribe(b =>
                {
                    CreatePanelComboBox.IsEnabled = b;
                    ToolTip.SetTip(this, b ? Language.AddPanelToolTip : Language.MaxPanelsAddedToolTip);
                })
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
