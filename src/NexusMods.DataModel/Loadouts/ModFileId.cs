using System.Text.Json;
using System.Text.Json.Serialization;
using Vogen;

namespace NexusMods.DataModel.Loadouts;

[ValueObject<Guid>(conversions: Conversions.None)]
[JsonConverter(typeof(ModFileIdConverter))]
public partial class ModFileId
{
    public static ModFileId New()
    {
        return From(Guid.NewGuid());
    }

    public class ModFileIdConverter : JsonConverter<ModFileId>
    {
        public override ModFileId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var data = reader.GetBytesFromBase64();
            return From(new Guid(data));
        }

        public override void Write(Utf8JsonWriter writer, ModFileId value, JsonSerializerOptions options)
        {
            Span<byte> span = stackalloc byte[16];
            value._value.TryWriteBytes(span);
            writer.WriteBase64StringValue(span);
        }
    }
}
