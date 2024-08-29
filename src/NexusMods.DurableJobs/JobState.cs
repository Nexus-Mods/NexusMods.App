using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Abstractions.DurableJobs;

namespace NexusMods.DurableJobs;

/// <summary>
/// Internal job state for a job. 
/// </summary>
[JsonConverter(typeof(JobStateSerializer))]
public class JobState
{
    /// <summary>
    /// The id of the job.
    /// </summary>
    public required JobId Id { get; init; }
    
    /// <summary>
    /// The job implementation.
    /// </summary>
    public required AJob Job { get; set; }
    
    /// <summary>
    /// If this is a sub job, this will contain the parent job id.
    /// </summary>
    public JobId ParentJobId { get; init; }
    
    /// <summary>
    /// If this is a sub job, this will contain the parent job's history index.
    /// </summary>
    public int ParentHistoryIndex { get; init; }
    
    /// <summary>
    /// The arguments that were passed to the job.
    /// </summary>
    public required object[] Arguments { get; init; } = [];

    /// <summary>
    /// The history of the job, each entry represents a child job that was run by this job.
    /// </summary>
    public List<HistoryEntry> History { get; set; } = new();
    
    /// <summary>
    /// The managing job manager.
    /// </summary>
    [JsonIgnore]
    public JobManager? Manager { get; set; }

    /// <summary>
    /// If provided, this will be called when the job is completed.
    /// </summary>
    [JsonIgnore]
    public Action<object, Exception?>? Continuation { get; set; }
}


internal class JobStateSerializer : JsonConverter<JobState>
{
    public override JobState? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();
        
        reader.Read();
        var id = JsonSerializer.Deserialize<JobId>(ref reader, options);
        reader.Read();
        var job = JsonSerializer.Deserialize<AJob>(ref reader, options);
        reader.Read();
        var parentJobId = JsonSerializer.Deserialize<JobId>(ref reader, options);
        reader.Read();
        var parentHistoryIndex = JsonSerializer.Deserialize<int>(ref reader, options);
        reader.Read();
        
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();
        
        reader.Read();

        var types = job!.ArgumentTypes;
        var arguments = GC.AllocateUninitializedArray<object>(types.Length);
        for (int i = 0; i < types.Length; i++)
        {
            var type = types[i];
            arguments[i] = JsonSerializer.Deserialize(ref reader, type, options)!;
            reader.Read();
        }
        reader.Read();
        
        var history = JsonSerializer.Deserialize<List<HistoryEntry>>(ref reader, options)!;
        reader.Read();
        
        return new JobState
        {
            Id = id,
            Job = job,
            ParentJobId = parentJobId,
            ParentHistoryIndex = parentHistoryIndex,
            Arguments = arguments,
            History = history,
        };
    }

    public override void Write(Utf8JsonWriter writer, JobState value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        JsonSerializer.Serialize(writer, value.Id, options);
        JsonSerializer.Serialize(writer, value.Job, options);
        JsonSerializer.Serialize(writer, value.ParentJobId, options);
        JsonSerializer.Serialize(writer, value.ParentHistoryIndex, options);
        JsonSerializer.Serialize(writer, value.Arguments, options);
        JsonSerializer.Serialize(writer, value.History, options);
        writer.WriteEndArray();
    }
}
