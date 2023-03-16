using JetBrains.Annotations;

namespace NexusMods.Paths;

public readonly partial struct AbsolutePath
{
    /// <inheritdoc cref="IFileSystem.GetFileEntry"/>
    [Obsolete($"This property is obsolete. Use IFileSystem.{nameof(IFileSystem.GetFileEntry)} directly.")]
    public IFileEntry FileInfo => _fileSystem.GetFileEntry(this);

    /// <inheritdoc cref="IFileSystem.FileExists"/>
    [Obsolete($"This property is obsolete. Use IFileSystem.{nameof(IFileSystem.FileExists)} directly.")]
    public bool FileExists => _fileSystem.FileExists(this);

    /// <summary>
    /// Obtains the name of the first folder stored in this path.
    /// </summary>
    public AbsolutePath TopParent
    {
        get
        {
            var thisPathLength = GetFullPathLength();
            var thisFullPath = thisPathLength <= 512 ? stackalloc char[thisPathLength] : GC.AllocateUninitializedArray<char>(thisPathLength);
            GetFullPath(thisFullPath);

            var index = thisFullPath.IndexOf(Path.DirectorySeparatorChar);
            if (OperatingSystem.IsLinux() && index == 0)
                return FromFullPath(DirectorySeparatorCharStr);

            var path = thisFullPath[..index];
            return FromDirectoryAndFileName(path.ToString(), "", _fileSystem);
        }
    }

    /// <inheritdoc cref="IFileSystem.OpenFile"/>
    [Obsolete($"This method is obsolete. Use IFileSystem.{nameof(IFileSystem.OpenFile)} directly.")]
    public Stream Open(FileMode mode, FileAccess access = FileAccess.Read, FileShare share = FileShare.ReadWrite)
        => _fileSystem.OpenFile(this, mode, access, share);

    /// <inheritdoc cref="IFileSystem.ReadFile"/>
    [Obsolete($"This method is obsolete. Use IFileSystem.{nameof(IFileSystem.ReadFile)} directly.")]
    public Stream Read() => _fileSystem.ReadFile(this);

    /// <inheritdoc cref="IFileSystem.CreateFile"/>
    [Obsolete($"This method is obsolete. Use IFileSystem.{nameof(IFileSystem.CreateFile)} directly.")]
    public Stream Create() => _fileSystem.CreateFile(this);

    /// <inheritdoc cref="IFileSystem.DeleteFile"/>
    [Obsolete($"This method is obsolete. Use IFileSystem.{nameof(IFileSystem.DeleteFile)} directly.")]
    public void Delete() => _fileSystem.DeleteFile(this);

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
                _fileSystem.MoveFile(this, dest, overwrite);
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
    [Obsolete($"This method is obsolete. Use IFileSystem.{nameof(IFileSystem.CreateDirectory)} directly.")]
    public void CreateDirectory() => _fileSystem.CreateDirectory(this);

    /// <inheritdoc cref="IFileSystem.DirectoryExists"/>
    [Obsolete($"This method is obsolete. Use IFileSystem.{nameof(IFileSystem.DirectoryExists)} directly.")]
    public bool DirectoryExists() => _fileSystem.DirectoryExists(this);

    /// <inheritdoc cref="IFileSystem.DeleteDirectory"/>
    [Obsolete($"This method is obsolete. Use IFileSystem.${nameof(IFileSystem.DeleteDirectory)} directly.")]
    public void DeleteDirectory(bool recursive = false) => _fileSystem.DeleteDirectory(this, recursive);

    /// <inheritdoc cref="IFileSystem.EnumerateFiles"/>
    [Obsolete($"This method is obsolete. Use IFileSystem.{nameof(IFileSystem.EnumerateFiles)} directly.")]
    public IEnumerable<AbsolutePath> EnumerateFiles(string pattern = "*", bool recursive = true)
        => _fileSystem.EnumerateFiles(this, pattern, recursive);

    /// <inheritdoc cref="IFileSystem.EnumerateFileEntries"/>
    [Obsolete($"This method is obsolete. Use IFileSystem.{nameof(IFileSystem.EnumerateFileEntries)} directly.")]
    public IEnumerable<IFileEntry> EnumerateFileEntries(string pattern = "*", bool recursive = true)
        => _fileSystem.EnumerateFileEntries(this, pattern, recursive);

    /// <inheritdoc cref="IFileSystem.EnumerateFiles"/>
    [Obsolete($"This method is obsolete. Use IFileSystem.{nameof(IFileSystem.EnumerateFiles)} directly.")]
    public IEnumerable<AbsolutePath> EnumerateFiles(Extension pattern, bool recursive = true)
        => EnumerateFiles("*" + pattern, recursive);

    /// <inheritdoc cref="IFileSystem.WriteAllTextAsync"/>
    [Obsolete($"This method is obsolete. Use IFileSystem.{nameof(IFileSystem.WriteAllTextAsync)} directly.")]
    public Task WriteAllTextAsync(string text, CancellationToken token = default)
        => _fileSystem.WriteAllTextAsync(this, text, token);

    /// <inheritdoc cref="IFileSystem.WriteAllLinesAsync"/>
    [Obsolete($"This method is obsolete. Use IFileSystem.{nameof(IFileSystem.WriteAllLinesAsync)} directly.")]
    public Task WriteAllLinesAsync([InstantHandle(RequireAwait = true)] IEnumerable<string> lines, CancellationToken token = default)
        => _fileSystem.WriteAllLinesAsync(this, lines, token);

    /// <inheritdoc cref="IFileSystem.ReadAllTextAsync"/>
    [Obsolete($"This method is obsolete. Use IFileSystem.{nameof(IFileSystem.ReadAllTextAsync)} directly.")]
    public Task<string> ReadAllTextAsync(CancellationToken token = default)
        => _fileSystem.ReadAllTextAsync(this, token);

    /// <inheritdoc cref="IFileSystem.WriteAllBytesAsync"/>
    [Obsolete($"This method is obsolete. Use IFileSystem.{nameof(IFileSystem.WriteAllBytesAsync)} directly.")]
    public Task WriteAllBytesAsync(byte[] data)
        => _fileSystem.WriteAllBytesAsync(this, data);
}
