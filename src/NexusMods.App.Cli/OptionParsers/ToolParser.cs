using NexusMods.Abstractions.Loadouts;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string into an <see cref="ITool"/>
/// </summary>
internal class ToolParser : IOptionParser<ITool>
{
    private readonly ITool[] _tools;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="tools"></param>
    public ToolParser(IEnumerable<ITool> tools) => _tools = tools.ToArray();


    public bool TryParse(string toParse, out ITool value, out string error)
    {
        value = _tools.First(g => g.Name == toParse);
        error = string.Empty;
        return true;
    }
}
