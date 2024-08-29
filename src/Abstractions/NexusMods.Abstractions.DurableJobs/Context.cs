using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.Abstractions.DurableJobs;

[JsonConverter(typeof(HistoryEntrySerializer))]
public record HistoryEntry
{
    /// <summary>
    /// The Id of the child job.
    /// </summary>
    public JobId ChildJobId { get; init; }
    
    /// <summary>
    /// The current state of the child job, if it's not completed yet it will be <see cref="JobState.Running"/> otherise it will be <see cref="JobState.Completed"/>
    /// or <see cref="JobState.Failed"/>.
    /// </summary>
    public JobStatus Status { get; set; }
    
    /// <summary>
    /// The job that will eventually return a result.
    /// </summary>
    public AJob? Job { get; set; }
    
    /// <summary>
    /// If the job is completed, this will contain the result of the job, if it's failed it will contain the exception.
    /// </summary>
    public object? Result { get; set; }
}


internal class HistoryEntrySerializer : JsonConverter<HistoryEntry>
{
    public override HistoryEntry? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();
        
        reader.Read();
        var childJobId = JsonSerializer.Deserialize<JobId>(ref reader, options);
        reader.Read();
        var status = JsonSerializer.Deserialize<JobStatus>(ref reader, options);
        reader.Read();
        var job = JsonSerializer.Deserialize<AJob>(ref reader, options);
        reader.Read();
        
        var result = JsonSerializer.Deserialize(ref reader, job!.ResultType, options);
        reader.Read();
        reader.Read();
        
        return new HistoryEntry
        {
            ChildJobId = childJobId,
            Status = status,
            Job = job,
            Result = result,
        };
    }

    public override void Write(Utf8JsonWriter writer, HistoryEntry value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        JsonSerializer.Serialize(writer, value.ChildJobId, options);
        JsonSerializer.Serialize(writer, value.Status, options);
        JsonSerializer.Serialize(writer, value.Job, options);
        JsonSerializer.Serialize(writer, value.Result, options);
        writer.WriteEndArray();
    }
}

public class Context
{
    public required IJobManager JobManager { get; init; }

    public required List<HistoryEntry> History { get; init; }
    public required JobId JobId { get; init; }
    public int ReplayIndex { get; set; } = 0;
    
}
