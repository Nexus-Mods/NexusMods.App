using Microsoft.Extensions.FileSystemGlobbing;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.CLI.OptionParsers;

internal class MatcherParser : IOptionParser<Matcher>
{
    /// <inheritdoc />
    public bool TryParse(string toParse, out Matcher value, out string error)
    {
        try
        {
            value = new Matcher();
            value.AddInclude(toParse);
            error = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            value = null!;
            error = exception.Message;
            return false;
        }
    }
}
