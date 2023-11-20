using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PageFactoryController
{
    private readonly ImmutableDictionary<PageFactoryId, IPageFactory> _factories;

    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public PageFactoryController(IEnumerable<IPageFactory> factories)
    {
        AssertUniqueness(factories);
        _factories = factories.ToImmutableDictionary(f => f.Id, f => f);
    }

    [Conditional("DEBUG")]
    private static void AssertUniqueness(IEnumerable<IPageFactory> factories)
    {
        var groups = factories.GroupBy(f => f.Id);
        foreach (var group in groups)
        {
            Debug.Assert(group.Count() == 1, $"{group.Key} shared by multiple factories: {group.Select(f => f.GetType().ToString()).Aggregate((a,b) => $"{a}\n{b}")}");
        }
    }

    public Page Create(PageData pageData)
    {
        if (!_factories.TryGetValue(pageData.FactoryId, out var factory))
            throw new KeyNotFoundException($"Unable to find registered factory with ID {pageData.FactoryId}");

        return factory.Create(pageData.Context);
    }

    public IEnumerable<PageDiscoveryDetails> GetAllDetails()
    {
        foreach (var kv in _factories)
        {
            var (factoryId, factory) = kv;
            var details = factory.GetDiscoveryDetails();

            foreach (var detail in details)
            {
                if (detail is null) continue;

                Debug.Assert(detail.PageData.FactoryId == factoryId);
                yield return detail;
            }
        }
    }
}
