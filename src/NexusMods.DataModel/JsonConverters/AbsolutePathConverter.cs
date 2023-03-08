using NexusMods.Paths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NexusMods.DataModel.JsonConverters;

public class AbsolutePathConverter : JsonConverter<AbsolutePath>
{
    public override AbsolutePath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return AbsolutePath.FromFullPath(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, AbsolutePath value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.GetFullPath());
    }
}
