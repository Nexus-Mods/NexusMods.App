using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI.Resources;
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

            this.WhenAnyValue(x =>
                    x.ViewModel!.InteractionControlViewModel.ValueContainer.HasChanged,
                    x => x.ViewModel!.Config.Options.RequiresRestart,
                    (hasChanged, requiresRestart) => requiresRestart && hasChanged
                )
                .BindToView(this, view => view.RequiresRestartBanner.IsVisible)
                .DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.DescriptionMarkdownRenderer, v => v.DescriptionMarkdownRendererViewModelViewHost.ViewModel)
                .DisposeWith(disposables);
        });
    }

    private void PopulateFromViewModel(ISettingEntryViewModel viewModel)
    {
        EntryName.Text = viewModel.Config.Options.DisplayName;
        RequiresRestartMessage.Text = viewModel.Config.Options.RestartMessage ?? Language.SettingEntryView_NeedRestartMessage;

        InteractionControl.ViewModel = viewModel.InteractionControlViewModel;

        LinkViewModel.ViewModel = viewModel.LinkRenderer;
        LinkViewModel.IsVisible = viewModel.LinkRenderer is not null;
    }
}
