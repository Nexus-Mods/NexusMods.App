using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Mutable variant of <see cref="IIndeterminateProgress"/>.
/// </summary>
[PublicAPI]
public interface IMutableIndeterminateProgress : IMutableProgress, IIndeterminateProgress;
