using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using NexusMods.Abstractions.Loadouts.Sorting;
using NexusMods.Paths;

namespace NexusMods.Games.CreationEngine;

public class PluginsFileWriter
{
    private record struct Row(ModKey Key, IReadOnlyList<MasterReference> References, List<ISortRule<Row, ModKey>> Rules);
    
    private readonly List<Row> _references = new();
    private readonly ISorter _sorter;

    public PluginsFileWriter(ISorter sorter)
    {
        _sorter = sorter;
    }

    public void Add(ModKey plugin, IReadOnlyList<MasterReference> masters)
    {
        var rules = new List<ISortRule<Row, ModKey>>();
        foreach (var master in masters)
            rules.Add(new After<Row, ModKey>() { Other = master.Master });
        _references.Add(new Row(plugin, masters, rules));
    }
    
    public void Write(StringWriter writer)
    {
        var sorted = _sorter.Sort(_references, static x => x.Key, static x => x.Rules); 
        
        throw new NotImplementedException();
    }
}
