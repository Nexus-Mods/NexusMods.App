namespace NexusMods.CLI.DataOutputs;

public record Table(IReadOnlyCollection<string> Columns,  IEnumerable<IEnumerable<object>> Rows);