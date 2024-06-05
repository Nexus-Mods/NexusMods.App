using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DynamicData;
using DynamicData.Binding;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;
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

            DataGrid.SelectedItemsToProperty(ViewModel!, vm => vm.SelectedModsObservable)
                .DisposeWith(d);

            DataGrid.Width = Double.NaN;

            AddModButton.Command = ViewModel!.AddMod;
            AddModAdvancedButton.Command = ViewModel!.AddModAdvanced;
            GetModsFromNexusButton.Command = ViewModel!.OpenNexusModPage;
            
            // Note: We get `StorageProvider` from Avalonia, using the View TopLevel.
            //       This is the suggested approach by an Avalonia team member.
            //       https://github.com/AvaloniaUI/Avalonia/discussions/10227
            GetModsFromDriveButton.Command = ReactiveCommand.CreateFromTask(
                async () => await ViewModel!.RegisterFromDisk(TopLevel.GetTopLevel(this)!.StorageProvider));
        });
    }
}

