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

            DataGrid.SelectedItemsToViewModelObservableChangeSetProperty(ViewModel!, vm => vm.SelectedModsObservable)
                .DisposeWith(d);

            // Synchronize the Grid with DataModel for Selected Items
            // Enable/Disable Add Mod & Add Mod Advanced Buttons
            void UpdateAddButtonState(bool isModAdded)
            {
                var enable = false;
                if (!isModAdded)
                    enable = ViewModel!.SelectedModsCollection.Count > 0 && 
                             !ViewModel!.SelectedModsCollection.Any(x => x.IsModAddedToLoadout);

                AddModButton.IsEnabled = enable;
                // Note Add(Advanced) should always be available.
            }

            ViewModel!.SelectedModsCollection.ObserveCollectionChanges()
                .Subscribe(_ => { UpdateAddButtonState(false); })
                .DisposeWith(d);
            ViewModel!.SelectedModsObservable.WhenValueChanged(model => model.IsModAddedToLoadout)
                .Subscribe(UpdateAddButtonState)
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

