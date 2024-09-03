using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.Abstractions.DurableJobs;

/// <summary>
/// A JSON serializer for IJob instances
/// </summary>
public class JobSerializer : JsonConverter<IJob>
{
    private readonly Dictionary<string, IJob> _jobs;

    public JobSerializer(IEnumerable<AOrchestration> jobs, IEnumerable<AUnitOfWork> unitsOfWork)
    {
        _jobs = jobs.OfType<IJob>().Concat(unitsOfWork).ToDictionary(j => j.GetType().FullName!);
    }

    public override IJob? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var name = reader.GetString();
        if (name is null)
            return null;
        return _jobs[name];
    }

    public override void Write(Utf8JsonWriter writer, IJob value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.GetType().FullName);
    }
}
