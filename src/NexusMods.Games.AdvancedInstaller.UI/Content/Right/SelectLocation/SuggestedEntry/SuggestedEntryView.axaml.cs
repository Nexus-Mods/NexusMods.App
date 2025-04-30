using System.Diagnostics.CodeAnalysis;
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
            // Title
            this.OneWayBind(ViewModel, vm => vm.Title,
                    v => v.LocationName.Text)
                .DisposeWith(disposables);

            // Subtitle
            this.OneWayBind(ViewModel, vm => vm.Subtitle,
                    v => v.LocationSubHeading.Text)
                .DisposeWith(disposables);

            // Create mapping command
            this.BindCommand(ViewModel, vm => vm.CreateMappingCommand,
                    v => v.SelectRoundedButton)
                .DisposeWith(disposables);
        });
    }
}
