using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace NexusMods.Networking.NexusWebApi.DTOs.OAuth;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MembershipRole
{
    [EnumMember(Value = "member")]
    Member,
    [EnumMember(Value = "supporter")]
    Supporter,
    [EnumMember(Value = "premium")]
    Premium,
    [EnumMember(Value = "lifetimepremium")]
    LifetimePremium,
}
