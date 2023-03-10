namespace NexusMods.Paths;

public partial class InMemoryFileSystem
{
    private class InMemoryDirectoryEntry : IDirectoryEntry
    {
        public AbsolutePath Path { get; }

        public InMemoryDirectoryEntry ParentDirectory { get; }

        public Dictionary<RelativePath, InMemoryFileEntry> Files { get; } = new();

        public Dictionary<RelativePath, InMemoryDirectoryEntry> Directories { get; } = new();

        public InMemoryDirectoryEntry(AbsolutePath path, InMemoryDirectoryEntry parentDirectory)
        {
            Path = path;
            ParentDirectory = parentDirectory;
        }
    }
}
