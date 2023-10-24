using System.Collections.Immutable;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PageFactoryController
{
    private readonly ImmutableDictionary<PageFactoryId, IPageFactory> _factories;

    public PageFactoryController(IEnumerable<IPageFactory> factories)
    {
        _factories = factories.ToImmutableDictionary(f => f.Id, f => f);
    }

    public Page Create(PageData pageData)
    {
        if (!_factories.TryGetValue(pageData.FactoryId, out var factory))
            throw new KeyNotFoundException($"Unable to find registered factory with ID {pageData.FactoryId}");

        return factory.Create(pageData.Parameter);
    }
}
