using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using OneOf;

namespace NexusMods.Abstractions.Jobs;
using Union = OneOf<IIndeterminateProgress, IDeterminateProgress>;

[PublicAPI]
public class Progress
{
    private readonly Union _value;

    /// <summary>
    /// Constructor.
    /// </summary>
    public Progress(Union value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets whether the progress is indeterminate.
    /// </summary>
    public bool IsIndeterminate => _value.IsT0;

    /// <summary>
    /// Gets whether the progress is determinate.
    /// </summary>
    public bool IsDeterminate => _value.IsT1;

    /// <summary>
    /// Gets the value as <see cref="IProgress"/>.
    /// </summary>
    /// <seealso cref="TryGetIndeterminateProgress"/>
    /// <seealso cref="TryGetDeterminateProgress"/>
    public IProgress Value => _value.IsT0 ? _value.AsT0 : _value.AsT1;

    /// <summary>
    /// Returns the progress as a <see cref="IIndeterminateProgress"/> using the try-get pattern.
    /// </summary>
    public bool TryGetIndeterminateProgress([NotNullWhen(true)] out IIndeterminateProgress? progress)
    {
        if (!_value.IsT0)
        {
            progress = null;
            return false;
        }

        progress = _value.AsT0;
        return true;
    }

    /// <summary>
    /// Returns the progress as a <see cref="IIndeterminateProgress"/> using the try-get pattern.
    /// </summary>
    public bool TryGetDeterminateProgress([NotNullWhen(true)] out IDeterminateProgress? progress)
    {
        if (!_value.IsT1)
        {
            progress = null;
            return false;
        }

        progress = _value.AsT1;
        return true;
    }
}
