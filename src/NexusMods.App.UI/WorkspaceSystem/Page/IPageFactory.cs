using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.App.UI.WorkspaceSystem;

/// <summary>
/// Represents a factory for creating pages.
/// </summary>
public interface IPageFactory
{
    /// <summary>
    /// Gets the unique identifier of this factory.
    /// </summary>
    public PageFactoryId Id { get; }

    /// <summary>
    /// Creates a new page using the provided context.
    /// </summary>
    public Page Create(IPageFactoryContext context);

    public IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails();
}

/// <summary>
/// Generic implementation of <see cref="IPageFactory"/>
/// </summary>
/// <typeparam name="TViewModel"></typeparam>
/// <typeparam name="TContext"></typeparam>
public interface IPageFactory<out TViewModel, in TContext> : IPageFactory
    where TViewModel : class, IPageViewModelInterface
    where TContext : class, IPageFactoryContext
{
    Page IPageFactory.Create(IPageFactoryContext context)
    {
        if (context is not TContext actualContext)
            throw new ArgumentException($"Unsupported type: {context.GetType()}");

        var vm = CreateViewModel(actualContext);
        return new Page
        {
            ViewModel = vm,
            PageData = new PageData
            {
                FactoryId = Id,
                Context = actualContext
            }
        };
    }

    /// <summary>
    /// Creates a new view model using the provided context.
    /// </summary>
    public TViewModel CreateViewModel(TContext parameter);
}

/// <summary>
/// Abstract class to easily implement <see cref="IPageFactory"/>.
/// </summary>
[PublicAPI]
public abstract class APageFactory<TViewModel, TContext> : IPageFactory<TViewModel, TContext>
    where TViewModel : class, IPageViewModelInterface
    where TContext : class, IPageFactoryContext
{
    /// <inheritdoc/>
    public abstract PageFactoryId Id { get; }

    protected readonly IServiceProvider ServiceProvider;
    protected readonly IWorkspaceController WorkspaceController;

    protected APageFactory(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        WorkspaceController = serviceProvider.GetRequiredService<IWorkspaceController>();
    }

    /// <inheritdoc/>
    public abstract TViewModel CreateViewModel(TContext parameter);

    /// <inheritdoc/>
    public virtual IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails() => Array.Empty<PageDiscoveryDetails?>();
}
