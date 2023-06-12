using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.CLI.OptionParsers;

public class AbsolutePathParser : IOptionParser<AbsolutePath>
{
    private readonly IFileSystem _fileSystem;

    public AbsolutePathParser(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public AbsolutePath Parse(string input, OptionDefinition<AbsolutePath> definition) => _fileSystem.FromUnsanitizedFullPath(input);

    public IEnumerable<string> GetOptions(string input) => Array.Empty<string>();
}
