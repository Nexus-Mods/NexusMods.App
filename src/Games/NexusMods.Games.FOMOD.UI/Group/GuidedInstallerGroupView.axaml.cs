using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.Common.GuidedInstaller;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public partial class GuidedInstallerGroupView : ReactiveUserControl<IGuidedInstallerGroupViewModel>
{
    public GuidedInstallerGroupView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            GroupName.Text = ViewModel?.Group.Name;
            GroupType.IsVisible = ViewModel?.Group.Type == OptionGroupType.AtLeastOne;

            this.OneWayBind(ViewModel, vm => vm.Options, view => view.OptionsListBox.ItemsSource)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.HighlightedOption, view => view.OptionsListBox.SelectedItem)
                .DisposeWith(disposables);
        });
    }
}
