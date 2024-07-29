using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using OneOf;

namespace NexusMods.Abstractions.Library.Installers;

/// <summary>
/// Represents the result for an installer.
/// </summary>
[PublicAPI]
public sealed class InstallerResult : OneOfBase<Success, NotSupported>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public InstallerResult(OneOf<Success, NotSupported> input) : base(input) { }

    /// <summary>
    /// Gets whether the result is <see cref="Success"/>.
    /// </summary>
    public bool IsSuccess => IsT0;

    /// <summary>
    /// Gets whether the result is <see cref="NotSupported"/>.
    /// </summary>
    public bool IsNotSupported => IsT1;

    /// <summary/>
    public static implicit operator InstallerResult(Success x) => new(x);

    /// <summary/>
    public static implicit operator InstallerResult(NotSupported x) => new(x);
}

/// <summary>
/// The input is supported by the installer.
/// </summary>
[PublicAPI]
public record Success;

/// <summary>
/// The input is not supported by the installer.
/// </summary>
[PublicAPI]
public record NotSupported;
