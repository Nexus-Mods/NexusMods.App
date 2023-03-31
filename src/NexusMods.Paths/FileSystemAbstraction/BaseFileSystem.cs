using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.HighPerformance.CommunityToolkit;

namespace NexusMods.Paths;

/// <summary>
/// Abstract class for implementations of <see cref="IFileSystem"/> that
/// provides helper functions and reduces code duplication.
/// </summary>
[PublicAPI]
public abstract class BaseFileSystem : IFileSystem
{
    private readonly Dictionary<AbsolutePath, AbsolutePath> _pathMappings = new();
    private readonly Dictionary<KnownPath, AbsolutePath> _knownPathMappings = new();

    private readonly bool _hasPathMappings;
    private readonly bool _convertCrossPlatformPaths;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected BaseFileSystem() { }

    /// <summary>
    /// Constructor that accepts path mappings.
    /// </summary>
    /// <param name="pathMappings"></param>
    /// <param name="knownPathMappings"></param>
    /// <param name="convertCrossPlatformPaths"></param>
    protected BaseFileSystem(
        Dictionary<AbsolutePath, AbsolutePath> pathMappings,
        Dictionary<KnownPath, AbsolutePath> knownPathMappings,
        bool convertCrossPlatformPaths)
    {
        _pathMappings = pathMappings;
        _knownPathMappings = knownPathMappings;
        _hasPathMappings = _pathMappings.Any();
        _convertCrossPlatformPaths = convertCrossPlatformPaths;
    }

    internal AbsolutePath GetMappedPath(AbsolutePath originalPath)
    {
        // fast exit
        if (!_hasPathMappings) return originalPath;

        // direct mapping
        if (_pathMappings.TryGetValue(originalPath, out var mappedPath))
            return mappedPath;

        // check if the path has already been mapped
        if (_pathMappings.FirstOrDefault(kv => originalPath.InFolder(kv.Value)).Key != default)
            return originalPath;

        // indirect mapping via parent directory
        var (originalParentDirectory, newParentDirectory) = _pathMappings
            .FirstOrDefault(kv => originalPath.InFolder(kv.Key));

        if (newParentDirectory == default) return originalPath;

        var relativePath = originalPath.RelativeTo(originalParentDirectory);
        var newPath = newParentDirectory.CombineUnchecked(relativePath);

        return newPath;
    }

    private string ConvertCrossPlatformPath(string input)
    {
        // NOTE(erri120): This method is required for
        // Windows (Wine) -> Linux (native) path mappings to work correctly.
        // This works on the assumption that the input is a fully rooted
        // Windows-drive path, eg: "C:\\foo\\bar" or "D:\\baz.txt" and
        // converts that input into a fully rooted Linux path, where
        // the first path component after the root directory is the
        // drive letter, eg: "C:\\foo\\bar" -> "/c/foo/bar"
        // This enables path mappings to map "/c" to something else, like
        // "/opt/wine/drive_c"

        if (!OperatingSystem.IsLinux()) return input;
        if (!_convertCrossPlatformPaths) return input;
        if (input.Length < 3) return input;
        var inputSpan = input.AsSpan();
        if (inputSpan.DangerousGetReferenceAt(1) != ':') return input;

        var result = string.Create(
            input.Length,
            input,
            (span, s) =>
            {
                // C:\foo\bar -> /c/foo/bar
                var inputSpan = s.AsSpan();

                span[0] = '/';
                span[1] = char.ToLower(inputSpan.DangerousGetReferenceAt(0));
                inputSpan.SliceFast(2).CopyTo(span.SliceFast(2));
                span.Replace('\\', '/', span);
            });

        return result;
    }

    #region IFileStream Implementation

    /// <inheritdoc/>
    public abstract IFileSystem CreateOverlayFileSystem(
        Dictionary<AbsolutePath, AbsolutePath> pathMappings,
        Dictionary<KnownPath, AbsolutePath> knownPathMappings,
        bool convertCrossPlatformPaths = false);

    /// <inheritdoc/>
    public virtual AbsolutePath GetKnownPath(KnownPath knownPath)
    {
        Debug.Assert(Enum.IsDefined(knownPath));

        if (_knownPathMappings.TryGetValue(knownPath, out var mappedPath))
            return mappedPath;

        AbsolutePath ThisOrDefault(string fullPath)
        {
            return string.IsNullOrWhiteSpace(fullPath) ? default : FromFullPath(fullPath);
        }

        // NOTE(erri120): if you change this method, make sure to update the docs in the KnownPath enum.
        var path = knownPath switch
        {
            KnownPath.EntryDirectory => FromFullPath(AppContext.BaseDirectory),
            KnownPath.CurrentDirectory => FromFullPath(Environment.CurrentDirectory),

            KnownPath.CommonApplicationDataDirectory => FromFullPath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)),
            KnownPath.ProgramFilesDirectory => ThisOrDefault(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)),
            KnownPath.ProgramFilesX86Directory => ThisOrDefault(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)),
            KnownPath.CommonProgramFilesDirectory => ThisOrDefault(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles)),
            KnownPath.CommonProgramFilesX86Directory => ThisOrDefault(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86)),

            KnownPath.TempDirectory => FromFullPath(Path.GetTempPath()),
            KnownPath.HomeDirectory => FromFullPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
            KnownPath.ApplicationDataDirectory => FromFullPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)),
            KnownPath.LocalApplicationDataDirectory => FromFullPath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)),
            KnownPath.MyDocumentsDirectory => FromFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),
            KnownPath.MyGamesDirectory => FromDirectoryAndFileName(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games"),
        };

        return path;
    }

    /// <summary>
    /// Returns a dictionary with the required path mappings and <see cref="KnownPath"/> mappings for Wine.
    /// </summary>
    /// <param name="fileSystem"></param>
    /// <param name="rootDirectory"></param>
    /// <param name="newHomeDirectory"></param>
    /// <returns></returns>
    public static (Dictionary<AbsolutePath, AbsolutePath> pathMappings, Dictionary<KnownPath, AbsolutePath> knownPathMappings) CreateWinePathMappings(
        IFileSystem fileSystem, AbsolutePath rootDirectory, AbsolutePath newHomeDirectory)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            throw new PlatformNotSupportedException();

        var pathMappings = new Dictionary<AbsolutePath, AbsolutePath>
        {
            { fileSystem.FromFullPath("/c"), rootDirectory },
        };

        var knownPaths = Enum.GetValues<KnownPath>();
        var knownPathMappings = new Dictionary<KnownPath, AbsolutePath>(knownPaths.Length);
        foreach (var knownPath in knownPaths)
        {
            var originalPath = fileSystem.GetKnownPath(knownPath);
            var newPath = knownPath switch
            {
                KnownPath.EntryDirectory => originalPath,
                KnownPath.CurrentDirectory => originalPath,

                KnownPath.CommonApplicationDataDirectory => rootDirectory.CombineUnchecked("ProgramData"),
                KnownPath.ProgramFilesDirectory => rootDirectory.CombineUnchecked("Program Files"),
                KnownPath.ProgramFilesX86Directory => rootDirectory.CombineUnchecked("Program Files (x86)"),
                KnownPath.CommonProgramFilesDirectory => rootDirectory.CombineUnchecked("Program Files/Common Files"),
                KnownPath.CommonProgramFilesX86Directory => rootDirectory.CombineUnchecked("Program Files (x86)/Common Files"),

                KnownPath.HomeDirectory => newHomeDirectory,
                KnownPath.MyDocumentsDirectory => newHomeDirectory.CombineUnchecked("Documents"),
                KnownPath.MyGamesDirectory => newHomeDirectory.CombineUnchecked("Documents/My Games"),
                KnownPath.LocalApplicationDataDirectory => newHomeDirectory.CombineUnchecked("AppData/Local"),
                KnownPath.ApplicationDataDirectory => newHomeDirectory.CombineUnchecked("AppData/Roaming"),
                KnownPath.TempDirectory => newHomeDirectory.CombineUnchecked("AppData/Local/Temp"),
            };

            knownPathMappings[knownPath] = newPath;
        }

        return (pathMappings, knownPathMappings);
    }

    /// <inheritdoc/>
    public AbsolutePath FromFullPath(string fullPath)
        => GetMappedPath(AbsolutePath.FromFullPath(ConvertCrossPlatformPath(fullPath), this));

    /// <inheritdoc/>
    public AbsolutePath FromDirectoryAndFileName(string directoryPath, string fullPath)
        => GetMappedPath(AbsolutePath.FromDirectoryAndFileName(ConvertCrossPlatformPath(directoryPath), fullPath, this));

    /// <inheritdoc/>
    public IFileEntry GetFileEntry(AbsolutePath path)
        => InternalGetFileEntry(GetMappedPath(path));

    /// <inheritdoc/>
    public IDirectoryEntry GetDirectoryEntry(AbsolutePath path)
        => InternalGetDirectoryEntry(GetMappedPath(path));

    /// <inheritdoc/>
    public IEnumerable<AbsolutePath> EnumerateRootDirectories()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();

        if (OperatingSystem.IsLinux())
            yield return FromFullPath("/");

        // go through the valid drive letters on Windows
        // or on Linux with cross platform path conversion enabled, in case
        // the current file system is an overlay file system for Wine
        if (OperatingSystem.IsWindows() || (OperatingSystem.IsLinux() && _convertCrossPlatformPaths))
        {
            for (var i = (uint)'A'; i <= 'Z'; i++)
            {
                var driveLetter = (char)i;
                var path = FromFullPath($"{driveLetter}:\\");
                if (DirectoryExists(path))
                    yield return path;
            }
        }
    }

    /// <inheritdoc/>
    public IEnumerable<AbsolutePath> EnumerateFiles(AbsolutePath directory, string pattern = "*", bool recursive = true)
        => InternalEnumerateFiles(GetMappedPath(directory), pattern, recursive);

    /// <inheritdoc/>
    public IEnumerable<AbsolutePath> EnumerateDirectories(AbsolutePath directory, string pattern = "*", bool recursive = true)
        => InternalEnumerateDirectories(GetMappedPath(directory), pattern, recursive);

    /// <inheritdoc/>
    public IEnumerable<IFileEntry> EnumerateFileEntries(AbsolutePath directory, string pattern = "*", bool recursive = true)
        => InternalEnumerateFileEntries(GetMappedPath(directory), pattern, recursive);

    /// <inheritdoc/>
    public Stream ReadFile(AbsolutePath path) => OpenFile(path, FileMode.Open, FileAccess.Read, FileShare.Read);

    /// <inheritdoc/>
    public Stream WriteFile(AbsolutePath path) => OpenFile(path, FileMode.Open, FileAccess.Write, FileShare.Read);

    /// <inheritdoc/>
    public Stream CreateFile(AbsolutePath path) => OpenFile(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

    /// <inheritdoc/>
    public Stream OpenFile(AbsolutePath path, FileMode mode, FileAccess access, FileShare share)
        => InternalOpenFile(GetMappedPath(path), mode, access, share);

    /// <inheritdoc/>
    public void CreateDirectory(AbsolutePath path)
        => InternalCreateDirectory(GetMappedPath(path));

    /// <inheritdoc/>
    public bool DirectoryExists(AbsolutePath path)
        => InternalDirectoryExists(GetMappedPath(path));

    /// <inheritdoc/>
    public void DeleteDirectory(AbsolutePath path, bool recursive)
        => InternalDeleteDirectory(GetMappedPath(path), recursive);

    /// <inheritdoc/>
    public bool FileExists(AbsolutePath path)
        => InternalFileExists(GetMappedPath(path));

    /// <inheritdoc/>
    public void DeleteFile(AbsolutePath path)
        => InternalDeleteFile(GetMappedPath(path));

    /// <inheritdoc/>
    public void MoveFile(AbsolutePath source, AbsolutePath dest, bool overwrite)
        => InternalMoveFile(GetMappedPath(source), GetMappedPath(dest), overwrite);

    /// <inheritdoc/>
    public async Task<byte[]> ReadAllBytesAsync(AbsolutePath path, CancellationToken cancellationToken = default)
    {
        await using var s = ReadFile(path);
        var length = s.Length;
        var bytes = GC.AllocateUninitializedArray<byte>((int)length);
        await s.ReadAtLeastAsync(bytes, bytes.Length, false, cancellationToken);
        return bytes;
    }

    /// <inheritdoc/>
    public async Task<string> ReadAllTextAsync(AbsolutePath path, CancellationToken cancellationToken = default)
    {
        return Encoding.UTF8.GetString(await ReadAllBytesAsync(path, cancellationToken));
    }

    /// <inheritdoc/>
    public async Task WriteAllBytesAsync(AbsolutePath path, byte[] data, CancellationToken cancellationToken = default)
    {
        await using var fs = CreateFile(path);
        await fs.WriteAsync(data, CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task WriteAllTextAsync(AbsolutePath path, string text, CancellationToken cancellationToken = default)
    {
        await using var fs = CreateFile(path);
        await fs.WriteAsync(Encoding.UTF8.GetBytes(text), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task WriteAllLinesAsync(AbsolutePath path, [InstantHandle(RequireAwait = true)] IEnumerable<string> lines, CancellationToken cancellationToken = default)
    {
        await using var fs = CreateFile(path);
        await using var sw = new StreamWriter(fs);
        foreach (var line in lines)
        {
            await sw.WriteLineAsync(line.AsMemory(), cancellationToken);
        }
    }

    #endregion

    #region Abstract Methods

    /// <inheritdoc cref="IFileSystem.GetFileEntry"/>
    protected abstract IFileEntry InternalGetFileEntry(AbsolutePath path);

    /// <inheritdoc cref="IFileSystem.GetDirectoryEntry"/>
    protected abstract IDirectoryEntry InternalGetDirectoryEntry(AbsolutePath path);

    /// <inheritdoc cref="IFileSystem.EnumerateFiles"/>
    protected abstract IEnumerable<AbsolutePath> InternalEnumerateFiles(AbsolutePath directory, string pattern, bool recursive);

    /// <inheritdoc cref="IFileSystem.EnumerateFiles"/>
    protected abstract IEnumerable<AbsolutePath> InternalEnumerateDirectories(AbsolutePath directory, string pattern, bool recursive);

    /// <inheritdoc cref="IFileSystem.EnumerateFiles"/>
    protected abstract IEnumerable<IFileEntry> InternalEnumerateFileEntries(AbsolutePath directory, string pattern, bool recursive);

    /// <inheritdoc cref="IFileSystem.OpenFile"/>
    protected abstract Stream InternalOpenFile(AbsolutePath path, FileMode mode, FileAccess access, FileShare share);

    /// <inheritdoc cref="IFileSystem.CreateDirectory"/>
    protected abstract void InternalCreateDirectory(AbsolutePath path);

    /// <inheritdoc cref="IFileSystem.DirectoryExists"/>
    protected abstract bool InternalDirectoryExists(AbsolutePath path);

    /// <inheritdoc cref="IFileSystem.DeleteDirectory"/>
    protected abstract void InternalDeleteDirectory(AbsolutePath path, bool recursive);

    /// <inheritdoc cref="IFileSystem.FileExists"/>
    protected abstract bool InternalFileExists(AbsolutePath path);

    /// <inheritdoc cref="IFileSystem.DeleteFile"/>
    protected abstract void InternalDeleteFile(AbsolutePath path);

    /// <inheritdoc cref="IFileSystem.MoveFile"/>
    protected abstract void InternalMoveFile(AbsolutePath source, AbsolutePath dest, bool overwrite);

    #endregion
}
