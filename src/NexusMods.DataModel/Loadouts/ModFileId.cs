using System.Text.Json.Serialization;
using NexusMods.DataModel.JsonConverters;
using Vogen;

namespace NexusMods.DataModel.Loadouts;

[ValueObject<Guid>(conversions: Conversions.None)]
[JsonConverter(typeof(ModFileIdConverter))]
public partial struct ModFileId
{
    public static ModFileId New()
    {
        return From(Guid.NewGuid());
    }
}
