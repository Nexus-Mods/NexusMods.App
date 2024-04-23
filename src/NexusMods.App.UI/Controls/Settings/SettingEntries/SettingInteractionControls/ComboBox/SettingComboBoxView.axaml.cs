using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

[UsedImplicitly]
public partial class SettingComboBoxView : ReactiveUserControl<ISettingComboBoxViewModel>
{
    public SettingComboBoxView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.DisplayItems, view => view.ComboBox.ItemsSource)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.SelectedItemIndex, view => view.ComboBox.SelectedIndex)
                .DisposeWith(disposables);
        });
    }
}
