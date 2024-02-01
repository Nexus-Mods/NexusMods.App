# Using `IFileSysten`

As part of [`0004-file-system-abstraction`](../decisions/backend/0004-file-system-abstraction.md), `NexusMods.Paths` now contains a file system abstraction:
`IFileSystem`.

New code should make use of this abstraction, as it is required for `Wine` (Windows on Linux) support.

## Getting Started

Using dependency-injection, simply add `IFileSystem` as a parameter to the constructor of your service:

```csharp
public class MyService
{
    private readonly IFileSystem _fileSystem;

    public MyService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<int> GetCharCount(AbsolutePath textFile, CancellationToken cancellationToken = default)
    {
        var text = await _fileSystem.ReadAllTextAsync(textFile, cancellationToken);
        return text.Length;
    }
}
```

!!! tip "When writing unit tests, make sure to import `NexusMods.Paths.TestingHelpers` in your test project."

And utilize `AutoFileSystem` from [AutoFixture](https://github.com/AutoFixture/AutoFixture):

```csharp
public class MyServiceTests
{
    [Theory, AutoFileSystem]
    public async Task Test_GetCharCount(InMemoryFileSystem fs, AbsolutePath textFile, string contents)
    {
        fs.AddFile(textFile, contents);

        var service = new MyService(fs);

        var result = await service.GetCharCount(textFile);
        result.Should.Be(contents.Length);
    }
}
```

## Accessing Special Folders

!!! warning "Everything file system and path related should be done using `IFileSystem`."

This includes getting the path to "special" folders.

Instead of using `Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)`, use `IFileSystem.GetKnownPath(KnownPath.ApplicationDataDirectory)`.

You should **never** use `Environment.GetFolderPath` together with `IFileSystem` because this will likely break path mappings.

!!! note "If `KnownPath` doesn't contain the path you need, open an issue or a PR to add this path."

### Path Re-mappings

Path mappings are designed to be invisible to the API consumer **and** the `IFileSystem` implementation,
they are completely isolated to `BaseFileSystem`.

This design enables us to properly support virtual file systems, like Wine prefixes.

If a tool requests the user's documents directory using `IFileSystem.GetKnownPath`, the implementation might
return:

- `C:\\Users\\{User}\\Documents`
- `/home/{User}/Documents`
- `/opt/wine/prefixes/my-cool-prefix/drive_c/Users/not-the-actual-user/Documents`.

The tool doesn't know, or care, what the actual concrete path is, the only thing it cares about,
is the fact that this is a path to the 'documents' folder.

!!! info "These mappings have to be manually created using `IFileSystem.CreateOverlayFileSystem`"
