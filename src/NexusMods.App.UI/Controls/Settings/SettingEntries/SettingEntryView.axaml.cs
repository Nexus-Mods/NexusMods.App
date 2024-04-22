using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

[UsedImplicitly]
public partial class SettingEntryView : ReactiveUserControl<ISettingEntryViewModel>
{
    public SettingEntryView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.ViewModel)
                .WhereNotNull()
                .Do(PopulateFromViewModel)
                .Subscribe()
                .DisposeWith(disposables);
        });
    }

    private void PopulateFromViewModel(ISettingEntryViewModel viewModel)
    {
        var descriptor = viewModel.PropertyUIDescriptor;

        InteractionControl.ViewModel = viewModel.InteractionControlViewModel;

        EntryName.Text = descriptor.DisplayName;
        EntryDescription.Text = descriptor.Description;

        // TODO: make this reactive
        RequiresRestartBanner.IsVisible = descriptor.RequiresRestart;
    }
}
