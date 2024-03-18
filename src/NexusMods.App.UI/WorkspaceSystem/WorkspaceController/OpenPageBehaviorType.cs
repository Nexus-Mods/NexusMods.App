using JetBrains.Annotations;

namespace NexusMods.App.UI.WorkspaceSystem;

/// <summary>
/// Mirrors <see cref="OpenPageBehavior"/> but without the data.
/// </summary>
/// <remarks>
/// This type can be used for serialization and settings. It allows
/// you to refer to one of the types in the <see cref="OpenPageBehavior"/>
/// union without having to specify the data.
///
/// This is more user-friendly that using the actual object type.
/// </remarks>
/// <seealso cref="OpenPageBehavior"/>
[PublicAPI]
public enum OpenPageBehaviorType : byte
{
    /// <summary>
    /// Represents <see cref="OpenPageBehavior.ReplaceTab"/>.
    /// </summary>
    ReplaceTab = 0,

    /// <summary>
    /// Represents <see cref="OpenPageBehavior.NewTab"/>.
    /// </summary>
    NewTab  = 1,

    /// <summary>
    /// Represents <see cref="OpenPageBehavior.NewPanel"/>.
    /// </summary>
    NewPanel = 2,
}
