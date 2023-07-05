namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// Base class for storing information related to file analysis results for
/// various kinds of mods/plugins.
/// </summary>
/// <remarks>
///     Implementation of <see cref="IFileAnalysisData"/> is context
///     (plugin/application) dependent. This interface does not define a contract,
///     it is only used for clarification and as a constraint
///     throughout the various DataModel APIs.
/// </remarks>
public interface IFileAnalysisData : IMetadata { }
