namespace NexusMods.CLI.OptionParsers;

public class VersionParser : IOptionParser<Version>
{
    public Version Parse(string input, OptionDefinition<Version> definition) => Version.Parse(input);

    public IEnumerable<string> GetOptions(string input) => Array.Empty<string>();
}