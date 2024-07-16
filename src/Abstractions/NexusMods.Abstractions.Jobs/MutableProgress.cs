using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;
using Union = OneOf.OneOf<IndeterminateProgress, DeterminateProgress>;

[PublicAPI]
public class MutableProgress : Progress
{
    private readonly Union _value;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MutableProgress(Union value) :
        base(value
            .MapT0<IIndeterminateProgress>(x => x)
            .MapT1<IDeterminateProgress>(x => x)
        )
    {
        _value = value;
    }

    /// <summary>
    /// Returns the progress as a <see cref="IndeterminateProgress"/> using the try-get pattern.
    /// </summary>
    public bool TryGetIndeterminateProgress([NotNullWhen(true)] out IndeterminateProgress? progress)
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
    /// Returns the progress as a <see cref="DeterminateProgress"/> using the try-get pattern.
    /// </summary>
    public bool TryGetDeterminateProgress([NotNullWhen(true)] out DeterminateProgress? progress)
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
