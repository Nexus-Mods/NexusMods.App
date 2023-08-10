using NexusMods.Abstractions.CLI;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string into a <see cref="Version"/>
/// </summary>
public class VersionParser : IOptionParser<Version>
{
    /// <inheritdoc />
    public Version Parse(string input, OptionDefinition<Version> definition) => Version.Parse(input);

    /// <inheritdoc />
    public IEnumerable<string> GetOptions(string input) => Array.Empty<string>();
}
