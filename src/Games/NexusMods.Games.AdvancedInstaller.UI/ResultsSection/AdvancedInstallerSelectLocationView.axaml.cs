using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public partial class AdvancedInstallerSelectLocationView : ReactiveUserControl<IAdvancedInstallerSelectLocationViewModel>
{
    public AdvancedInstallerSelectLocationView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel!.SuggestedEntries)
                .BindTo(this, view => view.SuggestedLocationItemsControl.ItemsSource)
                .DisposeWith(disposables);
        });
    }
}

