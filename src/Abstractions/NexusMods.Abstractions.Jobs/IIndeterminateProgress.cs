using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Indeterminate progress does not provide specific and measurable feedback,
/// and should be used for jobs where it's difficult to determine the
/// exact progress or completion time.
/// </summary>
[PublicAPI]
public interface IIndeterminateProgress : IProgress;
