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
    bool IsSupported(IWorkspaceContext context);

    /// <summary>
    /// Returns the title for the workspace context, or null if the factory does not support the given context.
    /// </summary>
    string? CreateTitle(IWorkspaceContext context);
    
    /// <summary>
    /// Returns the subtitle for the workspace context, or null if the factory does not support the given context.
    /// </summary>
    string? CreateSubtitle(IWorkspaceContext context);
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
    
    /// <inheritdoc/>
    string? IWorkspaceAttachmentsFactory.CreateSubtitle(IWorkspaceContext context)
    {
        return IsSupported(context) ? CreateSubtitle((TContext) context) : null;
    }

    /// <summary>
    /// Generic version of <see cref="IWorkspaceAttachmentsFactory.CreateTitle"/>, that makes implementing factories easier.
    /// </summary>
    string CreateTitle(TContext context);
    
    /// <summary>
    /// Generic version of <see cref="IWorkspaceAttachmentsFactory.CreateSubtitle"/>, that makes implementing factories easier.
    /// </summary>
    string CreateSubtitle(TContext context);
    
}
