using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.DataModel;

/// <summary>
/// Class responsible for managing the 'Archives' folders within the Nexus App for caching;
/// this includes accessing them by hash, caching them and pulling them by hash from the
/// cache.
/// </summary>
public class ArchiveManager : IArchiveManager
{
    private readonly HashSet<AbsolutePath> _locations;
    private readonly FileExtractor.FileExtractor _fileExtractor;
    private readonly FileContentsCache _contentsCache;

    /// <summary/>
    /// <param name="settings">Datamodel Settings</param>
    /// <param name="fileExtractor">Utility for extracting archives.</param>
    /// <param name="contentsCache">Cache of internal archive contents.</param>
    /// <remarks>Consider creating this class from DI.</remarks>
    public ArchiveManager(IDataModelSettings settings, FileExtractor.FileExtractor fileExtractor, FileContentsCache contentsCache)
    {
        _locations = settings.ArchiveLocations.Select(p => p.ToAbsolutePath()).ToHashSet();
        _contentsCache = contentsCache;
        _fileExtractor = fileExtractor;
        foreach (var folder in _locations)
            folder.CreateDirectory();
    }

    /// <summary>
    /// Stores a file with a given path inside one of the 'Archives' locations
    /// To fetch archived files, please use <see cref="PathFor"/> function.
    /// </summary>
    /// <param name="path">Path of the file to archive within the manager.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <param name="job">Job to which the operation gets reported to.</param>
    /// <returns></returns>
    /// <remarks>
    ///    This function performs a hash and copy at the same time; if file is
    ///    already archived, the result will be discarded.
    /// </remarks>
    public async Task<Hash> ArchiveFileAsync(AbsolutePath path, CancellationToken token = default, IJob<Size>? job = null)
    {
        Hash hash;
        var folder = SelectLocation(path);
        var tmpName = folder.CombineUnchecked(Guid.NewGuid().ToString().ToRelativePath().WithExtension(KnownExtensions.Tmp));
        {
            await using var tmpFile = tmpName.Create();
            await using var src = path.Read();
            hash = await src.HashingCopyAsync(tmpFile, token, job);
        }
        var finalName = folder.CombineUnchecked(NameForHash(hash));
        if (!finalName.FileExists)
            await tmpName.MoveToAsync(finalName);
        return hash;
    }

    /// <summary>
    /// Opens a file with a specified hash for reading.
    /// </summary>
    /// <param name="hash">The hash of the file to be opened.</param>
    public Stream OpenRead(Hash hash)
    {
        return PathFor(hash).Open(FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    /// <summary>
    /// Retrieves the path for a specified file, ensuring they exist.
    /// </summary>
    /// <param name="hash"></param>
    /// <exception cref="InvalidOperationException">No file with a matching hash was found.</exception>
    /// <returns>Absolute path for a given file.</returns>
    public AbsolutePath PathFor(Hash hash)
    {
        var rel = NameForHash(hash);
        return _locations.Select(r => r.CombineUnchecked(rel))
            .First(r => r.FileExists);
    }

    /// <summary>
    /// Determines if a given archive is stored by the manager.
    /// </summary>
    /// <param name="hash">The hash to determine if exists in any of the locations.</param>
    public bool HaveArchive(Hash hash)
    {
        // TODO: Provide location of found file as 'out' parameter. https://github.com/Nexus-Mods/NexusMods.App/issues/206#issue-1629712277
        var rel = NameForHash(hash);
        return _locations.Any(r => r.CombineUnchecked(rel).FileExists);
    }

    /// <summary>
    /// Determines if a given archive is stored by the manager, and returns the path if it is.
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public bool TryGetPathFor(Hash hash, out AbsolutePath path)
    {
        var relativePath = NameForHash(hash);
        if (_locations.Select(x => x.CombineUnchecked(relativePath)).TryGetFirst(x => x.FileExists, out path))
            return true;

        path = default;
        return false;
    }

    /// <summary>
    /// Returns true if the given hash is managed, or any archive that contains this file is managed
    /// </summary>
    /// <param name="hash">The hash to check.</param>
    public async ValueTask<bool> HaveFile(Hash hash)
    {
        if (HaveArchive(hash))
            return true;

        // Verify the archive which contains this hash still exists.
        foreach (var archive in _contentsCache.ArchivesThatContain(hash))
            if (await HaveFile(archive.Parent))
                return true;
        return false;
    }

    public Task BackupFiles(IEnumerable<(IStreamFactory, Hash, Size)> backups, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task ExtractFiles(IEnumerable<(Hash Src, IStreamFactory Dest)> files, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns a list of archives that contain a file with a specified hash.
    /// </summary>
    /// <param name="hash">The hash to search for.</param>
    public IEnumerable<HashRelativePath> ArchivesThatContain(Hash hash)
    {
        if (HaveArchive(hash))
            return new[] { new HashRelativePath(hash, default) };

        return _contentsCache.ArchivesThatContain(hash).Select(r => new HashRelativePath(r.Parent, r.Path));
    }

    /// <summary>
    /// Returns a list of all known archives based on their file cache.
    /// </summary>
    public HashSet<Hash> AllArchives()
    {
        return _locations.SelectMany(e => e.EnumerateFiles(KnownExtensions.Ra))
            .Select(a => Hash.FromHex(a.GetFileNameWithoutExtension()))
            .ToHashSet();
    }

    /// <summary>
    /// Extracts tje contents of an archive with the matching hash.
    /// </summary>
    /// <param name="hash">Hash of the file to extract.</param>
    /// <param name="select">The files to extract from this archive, separator/casing should match.</param>
    /// <param name="action">Function to apply to each entry in the archive.</param>
    /// <param name="token">Cancellation token for the process.</param>
    /// <returns>A Dictionary of RelativePath -> Return value from `func`</returns>
    /// <remarks>
    ///     Does not extract files to disk. If you need to save the data; do so in <paramref name="action"/>.
    ///     The source data passed to func can be in-memory.
    /// </remarks>
    public async Task ExtractAsync(Hash hash, IEnumerable<RelativePath> select, Func<RelativePath, IStreamFactory, ValueTask> action, CancellationToken token = default)
    {
        var paths = select.ToHashSet();
        await _fileExtractor.ForEachEntry(PathFor(hash), async (path, sFn) =>
        {
            if (paths.Contains(path))
                await action(path, sFn);

            return path;
        }, token);
    }

    // TODO: Ability to select location for archiving. https://github.com/Nexus-Mods/NexusMods.App/issues/213
    // ReSharper disable once UnusedParameter.Local
    private AbsolutePath SelectLocation(AbsolutePath path)
    {
        return _locations.First();
    }

    private static RelativePath NameForHash(Hash hash)
    {
        return hash.ToHex().ToRelativePath().WithExtension(KnownExtensions.Ra);
    }
}
