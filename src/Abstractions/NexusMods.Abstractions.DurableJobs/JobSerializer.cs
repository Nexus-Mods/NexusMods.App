using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.Abstractions.DurableJobs;

public class JobSerializer : JsonConverter<AJob>
{
    private readonly Dictionary<string, AJob> _jobs;

    public JobSerializer(IEnumerable<AJob> jobs)
    {
        _jobs = jobs.ToDictionary(j => j.GetType().FullName!);
    }

    public override AJob? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var name = reader.GetString();
        if (name is null)
            return null;
        return _jobs[name];
    }

    public override void Write(Utf8JsonWriter writer, AJob value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.GetType().FullName);
    }
}
