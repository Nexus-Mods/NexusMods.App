using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;
using NexusMods.App.UI.Resources;
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

            // Synchronize the Grid with DataModel for Selected Items
            Observable.FromEventPattern<SelectionChangedEventArgs>(
                    addHandler => DataGrid.SelectionChanged += addHandler,
                    removeHandler => DataGrid.SelectionChanged -= removeHandler)
                .Select(_ => DataGrid.SelectedItems.Cast<IFileOriginEntryViewModel>().ToList())
                .BindTo(ViewModel, vm => vm.SelectedMods)
                .DisposeWith(d);
            
            // Enable/Disable Add Mod & Add Mod Advanced Buttons
            this.WhenAnyValue(x => x.ViewModel!.SelectedMods.Count)
                .Select(count => count > 0)
                .BindTo(this, x => x.AddModButton.IsEnabled)
                .DisposeWith(d);

            this.WhenAnyValue(x => x.ViewModel!.SelectedMods.Count)
                .Select(count => count > 0)
                .BindTo(this, x => x.AddModAdvancedButton.IsEnabled)
                .DisposeWith(d);

            DataGrid.Width = Double.NaN;
            
            AddModButton.Command = ReactiveCommand.CreateFromTask(ViewModel!.AddMod);
            AddModAdvancedButton.Command = ReactiveCommand.CreateFromTask(ViewModel!.AddModAdvanced);
            GetModsFromNexusButton.Command = ReactiveCommand.CreateFromTask(ViewModel!.OpenNexusModPage);
            GetModsFromDriveButton.Command = ReactiveCommand.CreateFromTask(
                async () => await ViewModel!.RegisterFromDisk(TopLevel.GetTopLevel(this)!.StorageProvider));
        });
    }
}

