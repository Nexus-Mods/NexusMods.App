using System.Diagnostics;
using System.Text;
using NexusMods.Paths.Extensions;

namespace NexusMods.Paths;

/// <summary>
/// Extensions for absolute paths.
/// Functionality not directly tied to class but useful nonetheless.
/// </summary>
public partial struct AbsolutePath 
{
    /// <summary>
    /// Returns the file information for this file.
    /// </summary>
    public FileInfo FileInfo => _info ??= new FileInfo(ToString());
    
    /// <summary>
    /// Returns a <see cref="FileVersionInfo"/> representing the version information associated with the specified file.
    /// </summary>
    public FileVersionInfo VersionInfo => FileVersionInfo.GetVersionInfo(ToNativePath());
    
    /// <summary>
    /// Gets the size in bytes, of the current file.
    /// </summary>
    public Size Length => Size.From(FileInfo.Length);
    
    /// <summary>
    /// Retrieves the last time this file was written to in coordinated universal time (UTC).
    /// </summary>
    public DateTime LastWriteTimeUtc => FileInfo.LastWriteTimeUtc;
    
    /// <summary>
    /// Retrieves the creation time of this file in coordinated universal time (UTC).
    /// </summary>
    public DateTime CreationTimeUtc => FileInfo.CreationTimeUtc;
    
    /// <summary>
    /// Retrieves the last time this file was written to.
    /// </summary>
    public DateTime LastWriteTime => FileInfo.LastWriteTime;
    
    /// <summary>
    /// Retrieves the creation time of this file.
    /// </summary>
    public DateTime CreationTime => FileInfo.CreationTime;

    /// <summary>
    /// Returns true if the file exists, else false.
    /// </summary>
    public bool FileExists => Parts.Length != 0 && File.Exists(ToNativePath());
    
    /// <summary>
    /// Obtains the name of the first folder stored in this path.
    /// </summary>
    public AbsolutePath TopParent => PathFormat == PathFormat.Windows ? 
        new(Parts[..1], PathFormat) : 
        new AbsolutePath(Array.Empty<string>(), PathFormat);
    
    private FileInfo? _info = null;
    
    /// <summary>
    /// Opens a file stream to the given absolute path.
    /// </summary>
    public Stream Open(FileMode mode, FileAccess access = FileAccess.Read, FileShare share = FileShare.ReadWrite)
    {
        return File.Open(ToNativePath(), mode, access, share);
    }

    /// <summary>
    /// Opens this file for read-only access.
    /// </summary>
    public Stream Read()
    {
        return Open(FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    /// <summary>
    /// Creates a new file, overwriting one if it already existed.
    /// </summary>
    public Stream Create()
    {
        return Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);
    }

    /// <summary>
    /// Deletes the file.
    /// </summary>
    public void Delete()
    {
        var nativePath = ToNativePath();
        if (FileExists)
        {
            try
            {
                File.Delete(nativePath);
            }
            catch (UnauthorizedAccessException)
            {
                var fi = FileInfo;
                if (fi.IsReadOnly)
                {
                    fi.IsReadOnly = false;
                    File.Delete(nativePath);
                }
                else
                {
                    throw;
                }
            }
        }
        
        if (Directory.Exists(nativePath))
            DeleteDirectory();
    }

    /// <summary>
    /// Reads all of the data from this file into an array.
    /// </summary>
    /// <param name="token">Optional token to cancel this task.</param>
    /// <returns></returns>
    /// <remarks>
    ///    Supports max 2GB file size.
    /// </remarks>
    // ReSharper disable once MemberCanBePrivate.Global
    public async Task<byte[]> ReadAllBytesAsync(CancellationToken token = default)
    {
        await using var s = Read();
        var length = s.Length;
        var bytes = GC.AllocateUninitializedArray<byte>((int)length);
        await s.ReadAtLeastAsync(bytes, bytes.Length, false, token);
        return bytes;
    }
    
    /// <summary>
    /// Moves the current path to a new destination.
    /// </summary>
    /// <param name="dest">The destination to write to.</param>
    /// <param name="overwrite">True to overwrite existing file at destination, else false.</param>
    /// <param name="token">Token used for cancellation of the task.</param>
    public async ValueTask MoveToAsync(AbsolutePath dest, bool overwrite = true, CancellationToken? token = null)
    {
        var srcStr = ToNativePath();
        var destStr = dest.ToString();
        var fi = new FileInfo(srcStr);
        if (fi.IsReadOnly)
            fi.IsReadOnly = false;

        var fid = new FileInfo(destStr);
        if (dest.FileExists && fid.IsReadOnly)
            fid.IsReadOnly = false;

        var retries = 0;
        while (true)
        {
            try
            {
                File.Move(srcStr, destStr, overwrite);
                return;
            }
            catch (Exception)
            {
                if (retries > 10)
                    throw;
                
                retries++;
                await Task.Delay(TimeSpan.FromSeconds(1), token ?? CancellationToken.None);
            }
        }
    }

    /// <summary>
    /// Copies the contents of this file to the destination asynchronously.
    /// </summary>
    /// <param name="dest">The destination file.</param>
    /// <param name="token">[Optional] Use for cancelling the task.</param>
    public async ValueTask CopyToAsync(AbsolutePath dest, CancellationToken token = default)
    {
        await using var inf = Read();
        await using var ouf = dest.Create();
        await inf.CopyToAsync(ouf, token);
    }
    
    /// <summary>
    /// Copies the contents of <paramref name="src"/> into this path.
    /// </summary>
    /// <param name="src">The source stream to copy from.</param>
    /// <param name="token">Use this to cancel the task.</param>
    public async ValueTask CopyFromAsync(Stream src, CancellationToken token = default)
    {
        await using var output = Create();
        await src.CopyToAsync(output, token);
    }
    
    /// <summary>
    /// Creates a directory if it does not already exist.
    /// </summary>
    public void CreateDirectory() => Directory.CreateDirectory(ToNativePath());

    /// <summary>
    /// Deletes the directory specified by this absolute path.
    /// </summary>
    public readonly void DeleteDirectory(bool dontDeleteIfNotEmpty = false)
    {
        if (!DirectoryExists()) return;
        if (dontDeleteIfNotEmpty && (EnumerateFiles().Any() || EnumerateDirectories().Any())) return;
      
        foreach (var directory in Directory.GetDirectories(ToString()))
        {
            directory.ToAbsolutePath().DeleteDirectory(dontDeleteIfNotEmpty);
        }
        try
        {
            var di = new DirectoryInfo(ToNativePath());
            if (di.Attributes.HasFlag(FileAttributes.ReadOnly))
                di.Attributes &= ~FileAttributes.ReadOnly;
            
            Directory.Delete(ToString(), true);
        }
        catch (UnauthorizedAccessException)
        {
            Directory.Delete(ToString(), true);
        }
    }

    /// <summary>
    /// Returns true if this directory exists, else false.
    /// </summary>
    public readonly bool DirectoryExists() => Parts.Length != 0 && Directory.Exists(ToNativePath());

    /// <summary>
    /// Enumerates through all the files present in this directory.
    /// </summary>
    /// <param name="pattern">Pattern to search for files.</param>
    /// <param name="recursive">Whether the search should be done recursively or not.</param>
    /// <returns></returns>
    public readonly IEnumerable<AbsolutePath> EnumerateFiles(string pattern = "*", bool recursive = true)
    {
        return Directory.EnumerateFiles(ToNativePath(), pattern,
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Select(file => file.ToAbsolutePath());
    }
    
    /// <summary>
    /// Enumerates individual FileSystem entries for this directory.
    /// </summary>
    /// <param name="pattern">Pattern to search for files.</param>
    /// <param name="recursive">Whether the search should be done recursively or not.</param>
    /// <returns></returns>
    public IEnumerable<FileEntry> EnumerateFileEntries(string pattern = "*",
        bool recursive = true)
    {
        if (!DirectoryExists()) return Array.Empty<FileEntry>();
        return Directory.EnumerateFiles(ToNativePath(), pattern,
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Select(file =>
            {
                var path = file.ToAbsolutePath();
                var info = path.FileInfo;
                return new FileEntry(Path: path, Size: Size.From(info.Length), LastModified:info.LastWriteTimeUtc);
            });
    }

    /// <summary>
    /// Enumerates individual FileSystem entries for this directory where they match a specific extension.
    /// </summary>
    /// <param name="pattern">The extension to search for.</param>
    /// <param name="recursive">Whether the search should be done recursively or not.</param>
    /// <returns></returns>
    public IEnumerable<AbsolutePath> EnumerateFiles(Extension pattern,
        bool recursive = true)
    {
        return Directory.EnumerateFiles(ToString(), "*" + pattern, 
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Select(file => file.ToAbsolutePath());
    }
    
    /// <summary>
    /// Enumerates individual FileSystem directories under this directory.
    /// </summary>
    /// <param name="recursive">Whether to visit subdirectories or not.</param>
    public readonly IEnumerable<AbsolutePath> EnumerateDirectories(bool recursive = true)
    {
        if (!DirectoryExists()) return Array.Empty<AbsolutePath>();
        return Directory.EnumerateDirectories(ToNativePath(), "*",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Select(p => (AbsolutePath) p);
    }
    
    /// <summary>
    /// Writes all text specified in the given string to this path; using UTF-8 encoding.
    /// </summary>
    /// <param name="text">The text to write to the path.</param>
    /// <param name="token">Use this to cancel task if needed.</param>
    public async Task WriteAllTextAsync(string text, CancellationToken token = default)
    {
        await using var fs = Create();
        await fs.WriteAsync(Encoding.UTF8.GetBytes(text), token);
    }

    /// <summary>
    /// Writes all lines of text specified in the given collection to this path; using UTF-8 encoding.
    /// </summary>
    /// <param name="lines">The lines to write to the path.</param>
    /// <param name="token">Use this to cancel task if needed.</param>
    public async Task WriteAllLinesAsync(IEnumerable<string> lines, CancellationToken token = default)
    {
        await using var fs = Create();
        await using var sw = new StreamWriter(fs);
        foreach (var line in lines)
        {
            await sw.WriteLineAsync(line.AsMemory(), token);
        }
    }
    
    /// <summary>
    /// Reads all text from this absolute path, assuming UTF8 encoding.
    /// </summary>
    /// <param name="token">Use this to cancel task if needed.</param>
    public async Task<string> ReadAllTextAsync(CancellationToken token = default)
    {
        return Encoding.UTF8.GetString(await ReadAllBytesAsync(token));
    }

    /// <summary>
    /// Writes the specified byte array to the path.
    /// </summary>
    /// <param name="data">The array to write.</param>
    public async Task WriteAllBytesAsync(byte[] data)
    {
        await using var fs = Create();
        await fs.WriteAsync(data, CancellationToken.None);
    }
    
    private readonly string ToNativePath() => ToString();
}