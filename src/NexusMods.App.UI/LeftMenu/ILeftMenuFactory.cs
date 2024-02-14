using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.LeftMenu;

/// <summary>
/// Represents a factory for creating LeftMenu ViewModels for a workspace using the provided workspace context.
/// </summary>
public interface ILeftMenuFactory
{
    /// <summary>
    /// Creates a new LeftMenu view model using the given workspace context
    /// </summary>
    /// <param name="context">The WorkspaceContext used to determine the type of LeftMenu to return</param>
    /// <param name="workspaceId">Id of the workspace that the LeftMenu will open new tabs on</param>
    /// <param name="workspaceController">WorkspaceController to pass on to the LeftMenu</param>
    /// <returns></returns>
    public ILeftMenuViewModel? CreateLeftMenu(IWorkspaceContext context, WorkspaceId workspaceId,
        IWorkspaceController workspaceController);
}

/// <summary>
/// Generic version of <see cref="ILeftMenuFactory"/>, that makes implementing factories easier.
/// </summary>
/// <typeparam name="TContext">Type of Workspace Context</typeparam>
public interface ILeftMenuFactory<in TContext> : ILeftMenuFactory
    where TContext : class, IWorkspaceContext
{
    /// <Inheritdoc/>
    ILeftMenuViewModel? ILeftMenuFactory.CreateLeftMenu(IWorkspaceContext context, WorkspaceId workspaceId,
        IWorkspaceController workspaceController)
    {
        return context is not TContext actualContext
            ? null
            : CreateLeftMenuViewModel(actualContext, workspaceId, workspaceController);
    }

    /// <summary>
    /// Generic version of <see cref="ILeftMenuFactory.CreateLeftMenu"/>, that makes implementing factories easier.
    /// </summary>
    /// <param name="context">The WorkspaceContext used to determine the type of LeftMenu to return</param>
    /// <param name="workspaceId">Id of the workspace that the LeftMenu will open new tabs on</param>
    /// <param name="workspaceController">WorkspaceController to pass on to the LeftMenu</param>
    /// <returns></returns>
    ILeftMenuViewModel CreateLeftMenuViewModel(TContext context, WorkspaceId workspaceId,
        IWorkspaceController workspaceController);
}
