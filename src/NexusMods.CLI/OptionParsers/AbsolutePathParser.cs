using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string into an absolute path
/// </summary>
public class AbsolutePathParser : IOptionParser<AbsolutePath>
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="fileSystem"></param>
    public AbsolutePathParser(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public AbsolutePath Parse(string input, OptionDefinition<AbsolutePath> definition) => _fileSystem.FromUnsanitizedFullPath(input);

    /// <inheritdoc />
    public IEnumerable<string> GetOptions(string input) => Array.Empty<string>();
}
