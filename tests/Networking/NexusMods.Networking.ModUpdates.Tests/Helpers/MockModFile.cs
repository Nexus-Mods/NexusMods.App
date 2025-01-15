using NexusMods.Networking.ModUpdates.Traits;

/// <summary>
/// Represents a mock mod page for testing
/// </summary>
public class MockModPage
{
    private readonly List<MockModFile> _files = new();
    public IReadOnlyList<MockModFile> Files => _files;

    /// <summary>
    /// Adds a new file to this mod page
    /// </summary>
    public MockModFile AddFile(string name, string version, DateTimeOffset uploadedAt)
    {
        var file = new MockModFile(name, version, uploadedAt, this);
        _files.Add(file);
        return file;
    }

    /// <summary>
    /// Creates a new mod page with the specified files
    /// </summary>
    public static MockModPage Create(Action<MockModPage> configure)
    {
        var page = new MockModPage();
        configure(page);
        return page;
    }
}

/// <summary>
/// A mock implementation of IAmAModFile for testing purposes
/// </summary>
public class MockModFile : IAmAModFile
{
    public string Name { get; }
    public string Version { get; }
    public DateTimeOffset UploadedAt { get; }
    public IEnumerable<IAmAModFile> OtherFilesInSameModPage => 
        _modPage.Files.Where(f => f != this);

    private readonly MockModPage _modPage;

    internal MockModFile(string name, string version, DateTimeOffset uploadedAt, MockModPage modPage)
    {
        Name = name;
        Version = version;
        UploadedAt = uploadedAt;
        _modPage = modPage;
    }
}
