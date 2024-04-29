using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Humanizer.Bytes;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
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

    public ReadOnlyObservableCollection<IFileOriginEntryViewModel> FileOrigins { get; }

    public FileOriginsPageViewModel(
        IArchiveInstaller archiveInstaller,
        IFileOriginRegistry fileOriginRegistry,
        IWindowManager windowManager) : base(windowManager)
    {
        TabTitle = Language.FileOriginsPageTitle;
        TabIcon = IconValues.ModLibrary;
        
        var allFileOrigins = fileOriginRegistry.GetAll();

        FileOrigins = new ReadOnlyObservableCollection<IFileOriginEntryViewModel>(
            new ObservableCollection<IFileOriginEntryViewModel>(
                allFileOrigins.Select(fileOrigin =>
                    {
                        RelativePath name = fileOrigin.Contains(DownloadAnalysis.SuggestedName)
                            ? fileOrigin.Get(DownloadAnalysis.SuggestedName)
                            : fileOrigin.Get(FilePathMetadata.OriginalName);

                        return new FileOriginEntryViewModel
                        {
                            Name = name,
                            Size = fileOrigin.Size,
                            AddToLoadoutCommand = ReactiveCommand.CreateFromTask(async () =>
                                {
                                    await archiveInstaller.AddMods(LoadoutId, fileOrigin, name);
                                }
                            ),
                        };
                    }
                )
            )
        );
        
    }
}
