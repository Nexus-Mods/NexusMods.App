using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

[PublicAPI]
public interface IProgressRateFormatter
{
    /// <summary>
    /// Formats the given value into a user-friendly and localized string.
    /// </summary>
    string Format(double value);
}
