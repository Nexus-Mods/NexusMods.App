namespace NexusMods.Paths;

public partial class FileSystem
{
    private class FileEntry : IFileEntry
    {
        private readonly AbsolutePath _path;

        private FileInfo? _info;
        private FileInfo GetFileInfo() => _info ??= new FileInfo(_path.GetFullPath());

        /// <inheritdoc/>
        public IFileSystem FileSystem { get; set; }

        /// <inheritdoc/>
        public AbsolutePath Path => _path;

        /// <inheritdoc/>
        public Size Size => Size.FromLong(GetFileInfo().Length);

        /// <inheritdoc/>
        public DateTime LastWriteTime
        {
            get => GetFileInfo().LastWriteTime;
            set => GetFileInfo().LastWriteTime = value;
        }

        /// <inheritdoc/>
        public DateTime CreationTime
        {
            get => GetFileInfo().CreationTime;
            set => GetFileInfo().CreationTime = value;
        }

        /// <inheritdoc/>
        public bool IsReadOnly
        {
            get => GetFileInfo().IsReadOnly;
            set => GetFileInfo().IsReadOnly = value;
        }

        public FileEntry(IFileSystem fileSystem, AbsolutePath path)
        {
            FileSystem = fileSystem;
            _path = path;
        }

        /// <inheritdoc/>
        public FileVersionInfo GetFileVersionInfo()
        {
            var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(_path.GetFullPath());
            return new FileVersionInfo(FileVersionInfo.ParseVersionString(fvi.ProductVersion));
        }
    }
}
