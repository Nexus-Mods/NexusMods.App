using JetBrains.Annotations;

namespace NexusMods.Paths;

public readonly partial struct AbsolutePath
{
    /// <inheritdoc cref="IFileSystem.GetFileEntry"/>
    public IFileEntry FileInfo => FileSystem.GetFileEntry(this);

    /// <inheritdoc cref="IFileSystem.FileExists"/>
    public bool FileExists => FileSystem.FileExists(this);

    /// <inheritdoc cref="IFileSystem.OpenFile"/>
    public Stream Open(FileMode mode, FileAccess access = FileAccess.Read, FileShare share = FileShare.ReadWrite)
        => FileSystem.OpenFile(this, mode, access, share);

    /// <inheritdoc cref="IFileSystem.ReadFile"/>
    public Stream Read() => FileSystem.ReadFile(this);

    /// <inheritdoc cref="IFileSystem.CreateFile"/>
    public Stream Create() => FileSystem.CreateFile(this);

    /// <inheritdoc cref="IFileSystem.DeleteFile"/>
    public void Delete() => FileSystem.DeleteFile(this);

    /// <summary>
    /// Moves the current path to a new destination.
    /// </summary>
    /// <param name="dest">The destination to write to.</param>
    /// <param name="overwrite">True to overwrite existing file at destination, else false.</param>
    /// <param name="token">Token used for cancellation of the task.</param>
    public async ValueTask MoveToAsync(AbsolutePath dest, bool overwrite = true, CancellationToken token = default)
    {
        if (FileInfo.IsReadOnly)
            FileInfo.IsReadOnly = false;

        if (dest is { FileExists: true, FileInfo.IsReadOnly: true })
            dest.FileInfo.IsReadOnly = false;

        var retries = 0;
        while (true)
        {
            try
            {
                FileSystem.MoveFile(this, dest, overwrite);
                return;
            }
            catch (Exception)
            {
                if (retries > 10)
                    throw;

                retries++;
                await Task.Delay(TimeSpan.FromSeconds(1), token);
            }
        }
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

    /// <inheritdoc cref="IFileSystem.CreateDirectory"/>
    public void CreateDirectory() => FileSystem.CreateDirectory(this);

    /// <inheritdoc cref="IFileSystem.DirectoryExists"/>
    public bool DirectoryExists() => FileSystem.DirectoryExists(this);

    /// <inheritdoc cref="IFileSystem.DeleteDirectory"/>
    public void DeleteDirectory(bool recursive = false) => FileSystem.DeleteDirectory(this, recursive);

    /// <inheritdoc cref="IFileSystem.EnumerateFiles"/>
    public IEnumerable<AbsolutePath> EnumerateFiles(string pattern = "*", bool recursive = true)
        => FileSystem.EnumerateFiles(this, pattern, recursive);

    /// <inheritdoc cref="IFileSystem.EnumerateFileEntries"/>
    public IEnumerable<IFileEntry> EnumerateFileEntries(string pattern = "*", bool recursive = true)
        => FileSystem.EnumerateFileEntries(this, pattern, recursive);

    /// <inheritdoc cref="IFileSystem.EnumerateFiles"/>
    public IEnumerable<AbsolutePath> EnumerateFiles(Extension pattern, bool recursive = true)
        => EnumerateFiles("*" + pattern, recursive);

    /// <inheritdoc cref="IFileSystem.WriteAllTextAsync"/>
    public Task WriteAllTextAsync(string text, CancellationToken token = default)
        => FileSystem.WriteAllTextAsync(this, text, token);

    /// <inheritdoc cref="IFileSystem.WriteAllLinesAsync"/>
    public Task WriteAllLinesAsync([InstantHandle(RequireAwait = true)] IEnumerable<string> lines, CancellationToken token = default)
        => FileSystem.WriteAllLinesAsync(this, lines, token);

    /// <inheritdoc cref="IFileSystem.ReadAllTextAsync"/>
    public Task<string> ReadAllTextAsync(CancellationToken token = default)
        => FileSystem.ReadAllTextAsync(this, token);

    /// <inheritdoc cref="IFileSystem.WriteAllBytesAsync"/>
    public Task WriteAllBytesAsync(byte[] data)
        => FileSystem.WriteAllBytesAsync(this, data);

    /// <inheritdoc cref="IFileSystem.ReadAllBytesAsync"/>
    public Task<byte[]> ReadAllBytesAsync(CancellationToken token = default)
        => FileSystem.ReadAllBytesAsync(this, token);
}
