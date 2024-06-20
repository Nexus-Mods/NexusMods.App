using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Helpers;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ModLibrary;

public partial class FileOriginsPageView : ReactiveUserControl<IFileOriginsPageViewModel>
{
    public FileOriginsPageView()
    {
        InitializeComponent();
        
        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.FileOrigins, v => v.DataGrid.ItemsSource)
                .DisposeWith(d);

            DataGrid.SelectedItemsToProperty(ViewModel!, vm => vm.SelectedModsObservable)
                .DisposeWith(d);

            DataGrid.Width = Double.NaN;

            this.BindCommand(ViewModel, vm => vm.AddMod, view => view.AddModButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.AddModAdvanced, view => view.AddModAdvancedButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.OpenNexusModPage, view => view.GetModsFromNexusButton)
                .DisposeWith(d);
            
            this.BindCommand(ViewModel, vm => vm.OpenNexusModPage, view => view.EmptyLibraryLinkButton)
                .DisposeWith(d);
            
            this.BindCommand(ViewModel, vm => vm.OpenNexusModPage, view => view.OpenLinkBareIconButton)
                .DisposeWith(d);
            
            this.WhenAnyValue(view => view.ViewModel!.FileOrigins.Count)
                .Select(count => count == 0)
                .BindToView(this, view => view.EmptyState.IsActive)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.EmptyLibrarySubtitleText, view => view.EmptyLibrarySubtitleTextBlock.Text)
                .DisposeWith(d);

            // Note: We get `StorageProvider` from Avalonia, using the View TopLevel.
            //       This is the suggested approach by an Avalonia team member.
            //       https://github.com/AvaloniaUI/Avalonia/discussions/10227
            GetModsFromDriveButton.Command = ReactiveCommand.CreateFromTask(
                async () => await ViewModel!.RegisterFromDisk(TopLevel.GetTopLevel(this)!.StorageProvider));
        });
    }
}

