using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public partial class SettingEntryView : ReactiveUserControl<ISettingEntryViewModel>
{
    public SettingEntryView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel,
                        viewModel => viewModel.DisplayName,
                        view => view.EntryName.Text)
                    .DisposeWith(disposables);
                
                this.OneWayBind(ViewModel,
                        viewModel => viewModel.Description,
                        view => view.EntryDescription.Text)
                    .DisposeWith(disposables);
                
                this.OneWayBind(ViewModel,
                        viewModel => viewModel.InteractionControlViewModel,
                        view => view.InteractionControl.ViewModel)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                        viewModel => viewModel.RequiresRestart,
                        view => view.RequiresRestartBanner.IsVisible)
                    .DisposeWith(disposables);
            }
        );
    }
}
