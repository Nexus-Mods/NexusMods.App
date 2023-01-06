namespace NexusMods.CLI.DataOutputs;

/// <summary>
/// Container for telling renderers that data should be outputted in a table format.
/// </summary>
/// <param name="Columns"></param>
/// <param name="Rows"></param>
public record Table(IReadOnlyCollection<string> Columns,  IEnumerable<IEnumerable<object>> Rows);