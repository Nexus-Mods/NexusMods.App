using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DynamicData.Kernel;
using NexusMods.App.UI.Pages;
using NexusMods.App.UI.Windows;

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

    /// <summary>
    /// Returns the factory associated with the given ID.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when there is no registered factory with ID <see cref="PageData.FactoryId"/></exception>
    public IPageFactory GetFactory(PageData pageData)
    {
        if (!_factories.TryGetValue(pageData.FactoryId, out var factory))
            throw new KeyNotFoundException($"Unable to find registered factory with ID {pageData.FactoryId}");
        return factory;
    }

    /// <summary>
    /// Uses the factory with ID <see cref="PageData.FactoryId"/> to create a new page.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when there is no registered factory with ID <see cref="PageData.FactoryId"/></exception>
    public Page Create(PageData pageData, WindowId windowId, WorkspaceId workspaceId, PanelId panelId, Optional<PanelTabId> tabId)
    {
        var factory = GetFactory(pageData);
        var page = factory.Create(pageData.Context);

        page.ViewModel.WindowId = windowId;
        page.ViewModel.WorkspaceId = workspaceId;
        page.ViewModel.PanelId = panelId;

        if (tabId.HasValue)
        {
            page.ViewModel.TabId = tabId.Value;
        }

        return page;
    }

    /// <summary>
    /// Returns all <see cref="PageDiscoveryDetails"/> of every factory given a <see cref="IWorkspaceContext"/>
    /// </summary>
    /// <remarks>
    /// <see cref="PageDiscoveryDetails"/> are used to provide information about what pages the factories can
    /// create in a given <see cref="IWorkspaceContext"/>.
    /// </remarks>
    public IEnumerable<PageDiscoveryDetails> GetAllDetails(IWorkspaceContext workspaceContext)
    {
        foreach (var kv in _factories)
        {
            var (factoryId, factory) = kv;
            var details = factory.GetDiscoveryDetails(workspaceContext);

            foreach (var detail in details)
            {
                if (detail is null) continue;

                Debug.Assert(detail.PageData.FactoryId == factoryId);
                yield return detail;
            }
        }
    }
}
