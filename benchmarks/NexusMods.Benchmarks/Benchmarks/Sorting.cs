using BenchmarkDotNet.Attributes;
using NexusMods.Benchmarks.Interfaces;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Sorting;
using NexusMods.DataModel.Sorting.Rules;

namespace NexusMods.Benchmarks.Benchmarks;

[BenchmarkInfo("Sorting", "Tests how quickly it usually takes to sort the load order of X mods.")]
[MemoryDiagnoser]
public class Sorting : IBenchmark
{
    private List<Item> _rules;

    [Params(100, 1000, 2000, 5000, 10000)]
    public int NumItems { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(e => e.ToString());
        var numbers = Enumerable.Range(0, NumItems).Select(e => e.ToString());

        var rules = new List<Item>();
        foreach (var (n, idx) in letters.Select((n, idx) => (n, idx)))
        {
            if (idx == 0)
            {
                rules.Add(new Item { Id = n, Rules = new() { new First<Item, string>() } });
            }
            else
            {
                rules.Add(new Item
                {
                    Id = n, Rules = new()
                {
                    new First<Item, string>(),
                    new After<Item, string>(letters.ElementAt(idx - 1))
                }
                });
            }
        }

        foreach (var (n, idx) in numbers.Select((n, idx) => (n, idx)))
        {
            if (idx == 0)
            {
                rules.Add(new Item { Id = n, Rules = new() });
            }
            else
            {
                rules.Add(new Item
                {
                    Id = n, Rules = new()
                    {
                        new After<Item, string>(numbers.ElementAt(idx - 1))
                    }
                });
            }
        }

        _rules = Shuffle(rules).ToList();
    }

    [Benchmark]
    public void Sort()
    {
        Sorter.Sort<Item, string>(_rules, x => x.Id, x => x.Rules).ToArray();
    }

    private IEnumerable<Item> Shuffle(List<Item> rules)
    {
        var random = new Random();
        var n = rules.Count;
        while (n > 1)
        {
            n--;
            var k = random.Next(n + 1);
            (rules[k], rules[n]) = (rules[n], rules[k]);
        }

        return rules;
    }

    public class Item : IHasEntityId<string>
    {
        public string Id { get; init; } = string.Empty;
        public List<ISortRule<Item, string>> Rules { get; set; }
    }
}
