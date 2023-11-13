using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

[ExcludeFromCodeCoverage]
public partial class SuggestedEntryView : ReactiveUserControl<ISuggestedEntryViewModel>
{
    public SuggestedEntryView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind<ISuggestedEntryViewModel, SuggestedEntryView, string, string?>(ViewModel, vm => vm.Title,
                    v => v.LocationName.Text)
                .DisposeWith(disposables);

            this.OneWayBind<ISuggestedEntryViewModel, SuggestedEntryView, string, string?>(ViewModel, vm => vm.Subtitle,
                    v => v.LocationSubHeading.Text)
                .DisposeWith(disposables);

            this.BindCommand<SuggestedEntryView, ISuggestedEntryViewModel, ReactiveCommand<Unit, Unit>, Button>(
                    ViewModel, vm => vm.CreateMappingCommand, v => v.SelectRoundedButton)
                .DisposeWith(disposables);
        });
    }
}
