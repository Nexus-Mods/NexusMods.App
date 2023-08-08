using NexusMods.Abstractions.CLI;
using NexusMods.DataModel.Games;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string into an <see cref="ITool"/>
/// </summary>
public class ToolParser : IOptionParser<ITool>
{
    private readonly ITool[] _tools;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="tools"></param>
    public ToolParser(IEnumerable<ITool> tools) => _tools = tools.ToArray();

    /// <inheritdoc />
    public ITool Parse(string input, OptionDefinition<ITool> definition) => _tools.First(g => g.Name == input);

    /// <inheritdoc />
    public IEnumerable<string> GetOptions(string input)
    {
        var byName = _tools.Where(g => g.Name.Contains(input, StringComparison.InvariantCultureIgnoreCase));
        return byName.Select(t => t.Name);
    }
}
