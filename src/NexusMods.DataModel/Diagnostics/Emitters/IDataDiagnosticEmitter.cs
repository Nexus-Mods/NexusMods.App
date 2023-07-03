using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.Diagnostics.Emitters;

/// <summary>
/// Diagnoses data of type <typeparamref name="TData"/> and creates instances of <see cref="Diagnostic"/>.
/// </summary>
/// <typeparam name="TData">The data to diagnose.</typeparam>
[PublicAPI]
public interface IDataDiagnosticEmitter<in TData>
    where TData : Entity
{
    /// <summary>
    /// Diagnoses the data and creates instances of <see cref="Diagnostic"/>.
    /// </summary>
    IEnumerable<Diagnostic> Diagnose(TData data);
}
