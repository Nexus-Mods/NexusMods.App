using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;

/// <summary>
/// Describes the role the user has on the site.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MembershipRole>))]
public enum MembershipRole
{
    /// <summary>
    /// User is a regular member.
    /// </summary>
    [EnumMember(Value = "member")]
    Member,

    /// <summary>
    /// Supporter tier, like a mini-premium tier; from the older days.
    /// </summary>
    [EnumMember(Value = "supporter")]
    Supporter,

    /// <summary>
    /// User is on a premium subscription.
    /// </summary>
    [EnumMember(Value = "premium")]
    Premium,

    /// <summary>
    /// User has lifetime premium subscription. Either legacy or redeemed from Donation Points.
    /// </summary>
    [EnumMember(Value = "lifetimepremium")]
    LifetimePremium,
}
