using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.Abstractions.GuidedInstallers;
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
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Do(PopulateFromViewModel)
                .Subscribe()
                .DisposeWith(disposables);

            this.WhenAnyObservable(view => view.ViewModel!.HasValidSelectionObservable)
                .Subscribe(isValid =>
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
        GroupName.Text = viewModel.Group.Name;
        GroupType.IsVisible = viewModel.Group.Type == OptionGroupType.AtLeastOne;
    }
}
