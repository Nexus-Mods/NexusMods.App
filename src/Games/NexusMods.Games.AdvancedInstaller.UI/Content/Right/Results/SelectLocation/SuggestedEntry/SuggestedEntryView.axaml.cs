using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

[ExcludeFromCodeCoverage]
public partial class SuggestedEntryView : ReactiveUserControl<ISuggestedEntryViewModel>
{
    public SuggestedEntryView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Title, v => v.LocationName.Text)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Subtitle, v => v.LocationSubHeading.Text)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.SelectCommand, v => v.SelectRoundedButton)
                .DisposeWith(disposables);
        });
    }
}
