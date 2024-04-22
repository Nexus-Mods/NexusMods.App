using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Settings;

public partial class SettingsView : ReactiveUserControl<ISettingsViewModel>
{
    public SettingsView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            {
                this.BindCommand(ViewModel,
                        viewModel => viewModel.SaveCommand,
                        view => view.SaveButton)
                    .DisposeWith(d);

                this.BindCommand(ViewModel,
                        viewModel => viewModel.CancelCommand,
                        view => view.CancelButton)
                    .DisposeWith(d);

                this.BindCommand(ViewModel,
                        viewModel => viewModel.CloseCommand,
                        view => view.CloseButton)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel,
                        viewModel => viewModel.SettingEntries,
                        view => view.SettingEntriesItemsControl.ItemsSource)
                    .DisposeWith(d);
            }
        );
    }
}

