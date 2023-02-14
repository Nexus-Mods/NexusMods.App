using NexusMods.DataModel.Games;

namespace NexusMods.CLI.OptionParsers;

public class ToolParser : IOptionParser<ITool>
{
    private readonly ITool[] _tools;

    public ToolParser(IEnumerable<ITool> tools) => _tools = tools.ToArray();

    public ITool Parse(string input, OptionDefinition<ITool> definition) => _tools.First(g => g.Name == input);

    public IEnumerable<string> GetOptions(string input)
    {
        var byName = _tools.Where(g => g.Name.Contains(input, StringComparison.InvariantCultureIgnoreCase));
        return byName.Select(t => t.Name);
    }
}