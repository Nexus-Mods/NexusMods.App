using System.Text.RegularExpressions;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Extensions.BCL;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

/// <summary>
/// An abstract class that represents an emitter for known dependencies that are based on file contents. For example in the case of Cyberpunk 2077,
/// every shop that interfaces with Virtual Atelier, will include the text `VirtualShopRegistration` in some `.reds` file.
/// </summary>
public abstract class AFileStringContentsDependencyEmitter : APathBasedDependencyEmitterWithNexusDownload
{
    private readonly IFileStore _fileStore;

    public AFileStringContentsDependencyEmitter(IFileStore fileStore)
    {
        _fileStore = fileStore;
    }
    
    /// <summary>
    /// If any of these regexes match, the file is considered to require the dependency.
    /// </summary>
    public abstract Regex[] DependantRegexes { get; }


    protected override async ValueTask<bool> CheckIsDependant(LoadoutItemWithTargetPath.ReadOnly item)
    {
        if (!item.TryGetAsLoadoutFile(out var loadoutFile))
            return false;

        try
        {
            var data = await _fileStore.GetFileStream(loadoutFile.Hash);
            var content = await data.ReadAllTextAsync();
            return DependantRegexes.Any(regex => regex.IsMatch(content));
        }
        catch (Exception e)
        {
            return false;
        }

    }
}
