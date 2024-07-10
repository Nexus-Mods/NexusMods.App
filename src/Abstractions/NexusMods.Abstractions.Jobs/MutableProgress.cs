using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;
using Union = OneOf.OneOf<IMutableIndeterminateProgress, IMutableDeterminateProgress>;

[PublicAPI]
public class MutableProgress : Progress
{
    private readonly Union _value;

    /// <summary>
    /// Contructor.
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
    /// Gets the value as <see cref="IMutableProgress"/>.
    /// </summary>
    /// <seealso cref="TryGetIndeterminateProgress"/>
    /// <seealso cref="TryGetDeterminateProgress"/>
    public new IMutableProgress Value => _value.IsT0 ? _value.AsT0 : _value.AsT1;

    /// <summary>
    /// Returns the progress as a <see cref="IMutableIndeterminateProgress"/> using the try-get pattern.
    /// </summary>
    public bool TryGetIndeterminateProgress([NotNullWhen(true)] out IMutableIndeterminateProgress? progress)
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
    /// Returns the progress as a <see cref="IMutableDeterminateProgress"/> using the try-get pattern.
    /// </summary>
    public bool TryGetDeterminateProgress([NotNullWhen(true)] out IMutableDeterminateProgress? progress)
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
