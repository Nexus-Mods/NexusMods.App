using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using Humanizer;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public partial class ModInstalledView : ReactiveUserControl<IModInstalledViewModel>
{
    public ModInstalledView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.Installed)
                .CombineLatest(Observable.Interval(TimeSpan.FromSeconds(60)).StartWith(1))
                .Select(time => time.First.Humanize())
                .BindToUi(this, view => view.InstalledTextBlock.Text)
                .DisposeWith(d);
        });

    }
}

