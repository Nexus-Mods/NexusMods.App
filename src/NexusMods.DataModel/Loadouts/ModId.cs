using System.Text.Json.Serialization;
using NexusMods.DataModel.JsonConverters;
using Vogen;

namespace NexusMods.DataModel.Loadouts;

[ValueObject<Guid>(conversions: Conversions.None)]
[JsonConverter(typeof(ModIdConverter))]
public partial struct ModId
{
    public static ModId New()
    {
        return From(Guid.NewGuid());
    }
}
