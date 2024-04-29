using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Windows;

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

    /// <summary>
    /// Returns details about every page that can be created with this factory in the given <see cref="IWorkspaceContext"/>.
    /// </summary>
    public IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext);

    public Optional<OpenPageBehaviorType> DefaultOpenPageBehaviorWithData { get; }
    public Optional<OpenPageBehaviorType> DefaultOpenPageBehaviorWithoutData { get; }
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
                Context = actualContext,
            },
        };
    }

    /// <summary>
    /// Creates a new view model using the provided context.
    /// </summary>
    public TViewModel CreateViewModel(TContext context);
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
    protected readonly IWindowManager WindowManager;

    protected APageFactory(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        WindowManager = serviceProvider.GetRequiredService<IWindowManager>();
    }

    /// <inheritdoc/>
    public abstract TViewModel CreateViewModel(TContext context);

    /// <inheritdoc/>
    public virtual IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext) => Array.Empty<PageDiscoveryDetails?>();

    /// <inheritdoc/>
    public virtual Optional<OpenPageBehaviorType> DefaultOpenPageBehaviorWithData { get; } = Optional<OpenPageBehaviorType>.None;

    /// <inheritdoc/>
    public virtual Optional<OpenPageBehaviorType> DefaultOpenPageBehaviorWithoutData { get; } = Optional<OpenPageBehaviorType>.None;
}
