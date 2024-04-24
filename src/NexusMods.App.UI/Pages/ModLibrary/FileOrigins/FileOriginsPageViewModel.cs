using System.Collections.ObjectModel;
using Humanizer.Bytes;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ModLibrary;

public class FileOriginsPageViewModel : APageViewModel<IFileOriginsPageViewModel>, IFileOriginsPageViewModel
{
    public ReadOnlyObservableCollection<IFileOriginEntryViewModel> FileOrigins { get; }

    public FileOriginsPageViewModel(
        ILoadoutRegistry loadoutRegistry,
        IFileOriginRegistry fileOriginRegistry,
        IWindowManager windowManager) : base(windowManager)
    {
        
        
        var allFileOrigins = fileOriginRegistry.GetAll();
        FileOrigins = new ReadOnlyObservableCollection<IFileOriginEntryViewModel>(
            new ObservableCollection<IFileOriginEntryViewModel>(
                allFileOrigins.Select(fileOrigin => new FileOriginEntryViewModel
                {
                    Name = "TODO",
                    Size = ByteSize.FromBytes(fileOrigin.Size.Value).ToString(),
                    AddToLoadoutCommand = ReactiveCommand.Create(() =>
                    {
                        
                    }),
                })
            )
        );
    }
}
