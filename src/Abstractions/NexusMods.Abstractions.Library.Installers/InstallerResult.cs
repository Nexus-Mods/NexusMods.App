using JetBrains.Annotations;
using OneOf;

namespace NexusMods.Abstractions.Library.Installers;

/// <summary>
/// Represents the result for an installer.
/// </summary>
[PublicAPI]
public readonly struct InstallerResult
{
    private readonly OneOf<Success, NotSupported> _value;

    /// <summary>
    /// Constructor.
    /// </summary>
    public InstallerResult(OneOf<Success, NotSupported> input)
    {
        _value = input;
    }

    /// <summary>
    /// Gets whether the result is <see cref="Success"/>.
    /// </summary>
    public bool IsSuccess => _value.IsT0;

    /// <summary>
    /// Gets whether the result is <see cref="NotSupported"/>.
    /// </summary>
    public bool IsNotSupported(out string? reason)
    {
        if (!_value.IsT1)
        {
            reason = null;
            return false;
        }

        reason = _value.AsT1.Reason;
        return true;
    }

    /// <summary/>
    public static implicit operator InstallerResult(Success x) => new(x);

    /// <summary/>
    public static implicit operator InstallerResult(NotSupported x) => new(x);
}

/// <summary>
/// The input is supported by the installer.
/// </summary>
[PublicAPI]
public record struct Success;

/// <summary>
/// The input is not supported by the installer.
/// </summary>
[PublicAPI]
public record struct NotSupported(string? Reason = null);
