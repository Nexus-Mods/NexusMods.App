using JetBrains.Annotations;

namespace NexusMods.DataModel.Diagnostics;

/// <summary>
/// A specialized instance of <see cref="IObservable{T}"/> for <see cref="Diagnostic"/>.
/// </summary>
[PublicAPI]
public interface IDiagnosticObservable : IObservable<Diagnostic>
{
    /// <summary>
    /// Emits a new <see cref="Diagnostic"/>.
    /// </summary>
    void Emit(Diagnostic diagnostic);
}
