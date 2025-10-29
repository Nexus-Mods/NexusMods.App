using FluentAssertions;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Sorting;
using NexusMods.DataModel.Sorting;

namespace NexusMods.DataModel.Tests;

public class SortTests
{
    [Fact]
    public void CyclicDependency()
    {
        var items = new List<Item>()
        {
            new() { Id = "A", Rules = [new After<Item, string>() { Other = "B" }] },
            new() { Id = "B", Rules = [new After<Item, string>() { Other = "A" }] },
        };

        var act = () => new Sorter().Sort<Item, string>(items, x => x.Id, x => x.Rules).ToArray();

        act.Should().Throw<InvalidOperationException>().WithMessage("Cyclic dependency detected");
    }

    [Fact]
    public void MissingItem()
    {
        var items = new List<Item>()
        {
            new() { Id = "B", Rules = [
                new After<Item, string>() { Other = "A" },
                new Before<Item, string>() { Other = "C" },
            ]},
            new() { Id = "D", Rules = [new After<Item, string>() { Other = "B" }]},
            new() { Id = "E", Rules = [new After<Item, string>() { Other = "A" }]},
        };

        var act = () => new Sorter().Sort(items, x => x.Id, x => x.Rules).ToArray();
        act.Should().Throw<InvalidOperationException>().WithMessage("Cyclic dependency detected");
    }

    [Fact]
    public void FirstItemsComeFirst()
    {
        var data = new List<Item>
        {
            new() {Id = "B", Rules = new()},
            new()
            {Id = "A", Rules = new()
            {
                new First<Item, string>()
            }},
        };

        new Sorter().Sort<Item, string>(data, x => x.Id, x => x.Rules)
            .Select(i => i.Id)
            .Should().Equal("A", "B");
    }

    [Fact]
    public void FirstAndLast()
    {
        var data = new List<Item>
        {
            new() { Id = "B", Rules = [] },
            new() { Id = "C", Rules = [] },
            new() { Id = "D", Rules = [ new Last<Item, string>()] },
            new() { Id = "E", Rules = [] },
            new() { Id = "F", Rules = [] },
            new() { Id = "G", Rules = [] },
            new() { Id = "A", Rules = [ new First<Item, string>()] },
        };

        var res = new Sorter().Sort<Item, string>(data, x => x.Id, x => x.Rules)
            .Select(i => i.Id)
            .ToArray();

        res.First().Should().Be("A");
        res.Last().Should().Be("D");
    }

    [Fact]
    public void BeforeAndAfterWorks()
    {
        var data = new List<Item>
        {
            new() {Id = "B", Rules = new()},
            new()
            {Id = "A", Rules = new()
            {
                new Before<Item, string> { Other = "B" }
            }},
            new()
            {Id = "C", Rules = new()
            {
                new After<Item, string> { Other = "B" }
            }}
        };

        new Sorter().Sort<Item, string>(data, x => x.Id, x => x.Rules)
            .Select(i => i.Id)
            .Should().Equal("A", "B", "C");
    }

    [Fact]
    public void LargeComplexCollectionsCanBeSorted()
    {
        var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(e => e.ToString()).ToArray();
        var numbers = Enumerable.Range(0, 10000).Select(e => e.ToString()).ToArray();

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
                        new After<Item, string> { Other = letters.ElementAt(idx - 1)}
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
                        new After<Item, string> { Other = numbers.ElementAt(idx - 1)}
                    }
                });
            }
        }

        rules = Shuffle(rules).ToList();

        new Sorter().Sort<Item, string>(rules, x => x.Id, x => x.Rules)
            .Select(i => i.Id)
            .Should().Equal(letters.Concat(numbers));
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

        public List<ISortRule<Item, string>> Rules { get; set; } = new();
    }
}
