namespace NexusMods.Paths;

public partial class InMemoryFileSystem
{
    private class InMemoryFileEntry : IFileEntry
    {
        public byte[] Contents { get; set; }
        public InMemoryDirectoryEntry ParentDirectory { get; }

        public IFileSystem FileSystem { get; set; }

        public AbsolutePath Path { get; }

        public Size Size => Size.From(Contents.Length);

        public DateTime LastWriteTime { get; set; }

        public DateTime CreationTime { get; set; }

        public bool IsReadOnly { get; set; }

        public InMemoryFileEntry(IFileSystem fs, AbsolutePath path, InMemoryDirectoryEntry parentDirectory, byte[] contents)
        {
            FileSystem = fs;
            Path = path;
            ParentDirectory = parentDirectory;
            Contents = contents;
        }

        public FileVersionInfo GetFileVersionInfo()
        {
            throw new NotImplementedException();
        }
    }
}
