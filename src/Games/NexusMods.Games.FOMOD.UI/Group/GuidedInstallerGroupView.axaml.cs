using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

[UsedImplicitly]
public partial class GuidedInstallerGroupView : ReactiveUserControl<IGuidedInstallerGroupViewModel>
{
    public GuidedInstallerGroupView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            PopulateFromViewModel(ViewModel!);

            this.WhenAnyValue(x => x.ViewModel!.HasValidSelection)
                .SubscribeWithErrorLogging(isValid =>
                {
                    if (isValid)
                    {
                        GroupType.Classes.Remove("StatusDangerLighter");
                    }
                    else
                    {
                        GroupType.Classes.Add("StatusDangerLighter");
                    }
                })
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Options, view => view.OptionsListBox.ItemsSource)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.HighlightedOption, view => view.OptionsListBox.SelectedItem)
                .DisposeWith(disposables);
        });
    }

    private void PopulateFromViewModel(IGuidedInstallerGroupViewModel viewModel)
    {
        GroupName.Text = viewModel.Group.Name.ToUpperInvariant();
        GroupType.IsVisible = viewModel.Group.Type == OptionGroupType.AtLeastOne;
    }
}
