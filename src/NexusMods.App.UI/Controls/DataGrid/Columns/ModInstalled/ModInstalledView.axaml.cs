using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using Humanizer;
using NexusMods.Abstractions.Loadouts.Mods;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModInstalled;

public partial class ModInstalledView : ReactiveUserControl<IModInstalledViewModel>
{
    public ModInstalledView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.Value)
                .CombineLatest(Observable.Interval(TimeSpan.FromSeconds(60)).StartWith(1))
                .Select(time => time.First.Humanize())
                .OnUI()
                .BindTo<string, ModInstalledView, string>(this, view => view.InstalledTextBlock.Text)
                .DisposeWith(d);
        });

    }
}

