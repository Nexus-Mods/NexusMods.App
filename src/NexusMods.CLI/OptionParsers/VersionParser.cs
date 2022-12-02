namespace NexusMods.CLI.OptionParsers;

public class VersionParser : IOptionParser<Version>
{
    public Version Parse(string input, OptionDefinition<Version> definition)
    {
        return Version.Parse(input);
    }
}