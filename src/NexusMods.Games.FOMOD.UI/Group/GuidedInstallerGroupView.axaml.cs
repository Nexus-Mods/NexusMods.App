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
                        GroupTypeTextBlock.Classes.Remove("StatusDangerLighter");
                    }
                    else
                    {
                        GroupTypeTextBlock.Classes.Add("StatusDangerLighter");
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
        GroupNameTextBlock.Text = viewModel.Group.Name;
        GroupTypeTextBlock.IsVisible = viewModel.Group.Type == OptionGroupType.AtLeastOne;
    }
}
