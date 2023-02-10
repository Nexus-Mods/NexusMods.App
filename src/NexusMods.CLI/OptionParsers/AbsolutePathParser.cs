using NexusMods.Paths;

namespace NexusMods.CLI.OptionParsers;

public class AbsolutePathParser : IOptionParser<AbsolutePath>
{
    public AbsolutePath Parse(string input, OptionDefinition<AbsolutePath> definition) => input.ToAbsolutePath();

    public IEnumerable<string> GetOptions(string input) => Array.Empty<string>();
}