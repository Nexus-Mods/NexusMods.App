using System.Text.Json.Serialization;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(IVersionedObject))]
public partial class DataModelJsonContext : JsonSerializerContext
{
}