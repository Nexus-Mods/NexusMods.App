using Cloudtoid;
using FluentAssertions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Sorting;
using NexusMods.DataModel.Sorting.Rules;

namespace NexusMods.DataModel.Tests;

public class SortTests
{

    [Fact]
    public void FirstItemsComeFirst()
    {
        var data = new List<Item>
        {
            new Item {Id = "B", Rules = new()},
            new Item {Id = "A", Rules = new()
            {
                new First<Item, string>()
            }},
        };
        
        Sorter.Sort<Item, string>(data, x => x.Id, x => x.Rules)
            .Select(i => i.Id)
            .Should().BeEquivalentTo("A", "B");
    }
    
    [Fact]
    public void BeforeAndAfterWorks()
    {
        var data = new List<Item>
        {
            new Item {Id = "B", Rules = new()},
            new Item {Id = "A", Rules = new()
            {
                new Before<Item, string>("B")
            }},
            new Item {Id = "C", Rules = new()
            {
                new After<Item, string>("B")
            }}
        };
        
        Sorter.Sort<Item, string>(data, x => x.Id, x => x.Rules)
            .Select(i => i.Id)
            .Should().BeEquivalentTo("A", "B", "C");
    }
    
    [Fact]
    public void LargeComplexCollectionsCanBeSorted()
    {
        var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(e => e.ToString());
        var numbers = Enumerable.Range(0, 10000).Select(e => e.ToString());

        var rules = new List<Item>();
        foreach (var (n, idx) in letters.Select((n, idx) => (n, idx)))
        {
            if (idx == 0)
            {
                rules.Add(new Item {Id = n, Rules = new() {new First<Item, string>()}});
            }
            else
            {
                rules.Add(new Item {Id = n, Rules = new()
                {
                    new First<Item, string>(),
                    new After<Item, string>(letters.ElementAt(idx - 1))
                }});
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

        rules = Shuffle(rules).ToList();
        
        Sorter.Sort<Item, string>(rules, x => x.Id, x => x.Rules)
            .Select(i => i.Id)
            .Should().BeEquivalentTo(letters.Concat(numbers));
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