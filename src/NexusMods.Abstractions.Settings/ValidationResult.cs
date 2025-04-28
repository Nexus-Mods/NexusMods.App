using JetBrains.Annotations;
using OneOf;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Represents either a failed or successful validation result.
/// </summary>
[PublicAPI]
public readonly struct ValidationResult
{
    /// <summary>
    /// Union between <see cref="Failed"/> and <see cref="Successful"/>.
    /// </summary>
    public readonly OneOf<Failed, Successful> Value;

    /// <summary>
    /// Gets whether the validation failed.
    /// </summary>
    public bool IsFailed() => Value.IsT0;

    /// <summary>
    /// Gets whether the validation was successful.
    /// </summary>
    public bool IsSuccessful() => Value.IsT1;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ValidationResult(OneOf<Failed, Successful> value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a failed result with a reason.
    /// </summary>
    public static ValidationResult CreateFailed(string reason)
    {
        return new ValidationResult(new Failed(reason));
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ValidationResult CreateSuccessful()
    {
        return new ValidationResult(Successful.Instance);
    }

    /// <summary>
    /// Represents a failed validation.
    /// </summary>
    [PublicAPI]
    public readonly struct Failed
    {
        /// <summary>
        /// Reason why the validation failed.
        /// </summary>
        public readonly string Reason;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Failed(string reason)
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// Represents a successful validation.
    /// </summary>
    [PublicAPI]
    public readonly struct Successful
    {
        /// <summary>
        /// Instance.
        /// </summary>
        public static readonly Successful Instance;
    }
}
