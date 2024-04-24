using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ModLibrary;

public partial class FileOriginsPageView : ReactiveUserControl<IFileOriginsPageViewModel>
{
    public FileOriginsPageView()
    {
        InitializeComponent();
        
        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel,
                    vm => vm.FileOrigins,
                    v => v.DataGrid.ItemsSource)
                .DisposeWith(d);
            
            DataGrid.Width = Double.NaN;
        });
    }
}

