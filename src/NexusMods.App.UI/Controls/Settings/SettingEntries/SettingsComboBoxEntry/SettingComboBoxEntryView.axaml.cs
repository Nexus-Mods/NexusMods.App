using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries.SettingsComboBoxEntry;

public partial class SettingComboBoxEntryView : ReactiveUserControl<ISettingComboBoxControlViewModel>
{
    public SettingComboBoxEntryView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel,
                    viewModel => viewModel.Items,
                    view => view.ComboBox.ItemsSource)
                .DisposeWith(disposables);
            
            this.Bind(ViewModel,
                    viewModel => viewModel.SelectedItem,
                    view => view.ComboBox.SelectedItem)
                .DisposeWith(disposables);
        });
    }
}

