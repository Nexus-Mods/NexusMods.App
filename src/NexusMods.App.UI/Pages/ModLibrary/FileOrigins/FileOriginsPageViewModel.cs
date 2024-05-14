using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
using Humanizer.Bytes;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.ModLibrary;

public class FileOriginsPageViewModel : APageViewModel<IFileOriginsPageViewModel>, IFileOriginsPageViewModel
{
    [Reactive] public LoadoutId LoadoutId { get; set; }

    public ReadOnlyObservableCollection<IFileOriginEntryViewModel> FileOrigins => _fileOrigins;
    private readonly ReadOnlyObservableCollection<IFileOriginEntryViewModel> _fileOrigins;
    public FileOriginsPageViewModel(
        IArchiveInstaller archiveInstaller,
        IFileOriginRegistry fileOriginRegistry,
        IRepository<DownloadAnalysis.Model> downloadAnalysisRepository,
        IWindowManager windowManager) : base(windowManager)
    {
        TabTitle = Language.FileOriginsPageTitle;
        TabIcon = IconValues.ModLibrary;
        
        var downloadAnalyses = downloadAnalysisRepository.Observable;
        
        var entriesObservable = downloadAnalyses.ToObservableChangeSet()
            .Transform(fileOrigin => (IFileOriginEntryViewModel)new FileOriginEntryViewModel
            {
                Name = fileOrigin.SuggestedName,
                Size = fileOrigin.Size,
                AddToLoadoutCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        await archiveInstaller.AddMods(LoadoutId, fileOrigin);
                    }
                ),
            })
            .OnUI()
            .Bind(out _fileOrigins);
        
        this.WhenActivated(d =>
        {
            entriesObservable.Subscribe()
                .DisposeWith(d);
        });

        
        
    }
}
