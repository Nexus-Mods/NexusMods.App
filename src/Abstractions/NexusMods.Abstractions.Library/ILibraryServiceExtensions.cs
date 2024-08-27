using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Library;


// ReSharper disable once InconsistentNaming
/// <summary>
/// Extension methods for <see cref="ILibraryService"/>
/// </summary>
public static class ILibraryServiceExtensions
{
    /// <summary>
    /// Adds a local file to the library and installs it.
    /// </summary>
    public static async Task<LocalFile.ReadOnly> AddLocalFileAndWait(this ILibraryService libraryService, AbsolutePath file, CancellationToken token = default)
    {
        await using var job = libraryService.AddLocalFile(file);
        await job.StartAsync(token);
        await job.WaitToFinishAsync(token);
            
        if (!job.Result!.TryGetCompleted(out var completed))
            throw new Exception("Failed to store the file");
            
        if (!completed.TryGetData<LocalFile.ReadOnly>(out var localFile))
            throw new Exception("Failed to store the file");

        return localFile;
    }
    
    /// <summary>
    /// Adds a local file to the library and installs it.
    /// </summary>
    public static async Task<LoadoutItemGroup.ReadOnly> InstallItemAndWait(this ILibraryService libraryService, LibraryItem.ReadOnly item, LoadoutId targetLoadout, ILibraryItemInstaller? installer = null, CancellationToken token = default)
    {
        await using var installJob = libraryService.InstallItem(item, targetLoadout, installer);
        await installJob.StartAsync(token);
        await installJob.WaitToFinishAsync(token);
        
        if (!installJob.Result!.TryGetCompleted(out var installCompleted))
            throw new Exception("Failed to install the file");
        
        if (!installCompleted.TryGetData<LoadoutItemGroup.ReadOnly>(out var loadoutItemGroup))
            throw new Exception("Failed to install the file");
        
        return loadoutItemGroup;
    }
}
