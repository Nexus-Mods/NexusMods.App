using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.WorkspaceAttachments;

/// <summary>
/// Represents a factory for creating components or data that are specific to a workspace using the provided workspace context.
/// </summary>
public interface IWorkspaceAttachmentsFactory
{
    /// <summary>
    /// Returns whether the factory supports the given workspace context.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    bool IsSupported(IWorkspaceContext context);

    /// <summary>
    /// Returns the title for the workspace context, or null if the factory does not support the given context.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    string? CreateTitle(IWorkspaceContext context);
}


public interface IWorkspaceAttachmentsFactory<TContext> : IWorkspaceAttachmentsFactory
    where TContext : class, IWorkspaceContext
{
    /// <inheritdoc/>
    bool IWorkspaceAttachmentsFactory.IsSupported(IWorkspaceContext context)
    {
        return context is TContext;
    }

    /// <inheritdoc/>
    string? IWorkspaceAttachmentsFactory.CreateTitle(IWorkspaceContext context)
    {
        return IsSupported(context) ? CreateTitle((TContext) context) : null;
    }

    /// <summary>
    /// Generic version of <see cref="IWorkspaceAttachmentsFactory.CreateTitle"/>, that makes implementing factories easier.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    string CreateTitle(TContext context);
}
