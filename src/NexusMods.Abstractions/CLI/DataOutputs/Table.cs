namespace NexusMods.Abstractions.CLI.DataOutputs;

/// <summary>
/// Container for telling renderers that data should be outputted in a table format.
/// </summary>
/// <param name="Columns"></param>
/// <param name="Rows"></param>
/// <param name="Title"></param>
public record Table(IReadOnlyCollection<string> Columns, IEnumerable<IEnumerable<object>> Rows, string? Title = null);
