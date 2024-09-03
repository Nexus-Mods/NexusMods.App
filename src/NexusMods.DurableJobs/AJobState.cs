using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Abstractions.DurableJobs;

namespace NexusMods.DurableJobs;

[JsonConverter(typeof(JobStateSerializer))]
public abstract class AJobState
{
    /// <summary>
    /// The managing job manager.
    /// </summary>
    [JsonIgnore]
    public JobManager? Manager { get; set; }
    
    /// <summary>
    /// The id of the job.
    /// </summary>
    public required JobId Id { get; init; }
    
    /// <summary>
    /// The job implementation.
    /// </summary>
    public required IJob Job { get; set; }
    
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
    /// If provided, this will be called when the job is completed.
    /// </summary>
    [JsonIgnore]
    public Action<object, Exception?>? Continuation { get; set; }
}



internal class JobStateSerializer : JsonConverter<AJobState>
{
    public override AJobState? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();
        
        reader.Read();
        var id = JsonSerializer.Deserialize<JobId>(ref reader, options);
        reader.Read();
        var job = JsonSerializer.Deserialize<IJob>(ref reader, options);
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

        if (job is AOrchestration aJob)
        {

            var history = JsonSerializer.Deserialize<List<HistoryEntry>>(ref reader, options)!;
            reader.Read();

            return new OrchestrationState
            {
                Id = id,
                Job = aJob,
                ParentJobId = parentJobId,
                ParentHistoryIndex = parentHistoryIndex,
                Arguments = arguments,
                History = history,
            };
        }
        else if (job is AUnitOfWork uow)
        {
            return new UnitOfWorkState()
            {
                Id = id,
                Job = uow,
                ParentJobId = parentJobId,
                ParentHistoryIndex = parentHistoryIndex,
                Arguments = arguments,
                CancellationTokenSource = new CancellationTokenSource(),
            };
        }
        else
        {
            throw new InvalidOperationException("Unknown job type");
        }
    }

    public override void Write(Utf8JsonWriter writer, AJobState value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        JsonSerializer.Serialize(writer, value.Id, options);
        JsonSerializer.Serialize(writer, value.Job, options);
        JsonSerializer.Serialize(writer, value.ParentJobId, options);
        JsonSerializer.Serialize(writer, value.ParentHistoryIndex, options);
        JsonSerializer.Serialize(writer, value.Arguments, options);
        
        if (value is OrchestrationState jobState)
            JsonSerializer.Serialize(writer, jobState.History, options);
        writer.WriteEndArray();
    }
}
