using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NexusMods.Abstractions.NexusWebApi.DTOs.Interfaces;
using UserId = NexusMods.Abstractions.NexusWebApi.Types.UserId;

// ReSharper disable InconsistentNaming

namespace NexusMods.Abstractions.NexusWebApi.DTOs;

/// <summary>
/// Contains the current user's details.
/// Returned from an API call used to validate API key.
/// </summary>
public class ValidateInfo : IJsonSerializable<ValidateInfo>
{
    /// <summary>
    /// For deserialization only; please use <see cref="UserId"/>.
    /// </summary>
    [JsonPropertyName("user_id")]
    public ulong _UserId { get; set; }

    /// <summary>
    /// Unique identifier for the current user.
    /// </summary>
    public UserId UserId => UserId.From(_UserId);

    /// <summary>
    /// The API key associated with this request.
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    /// <summary>
    /// The user's display name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// <see cref="IsPremium"/>
    /// </summary>
    [Obsolete($"Use {nameof(IsPremium)} instead.")]
    [JsonPropertyName("is_premium?")]
    public bool _IsPremium { get; set; }

    /// <summary>
    /// <see cref="IsSupporter"/>
    /// </summary>
    [Obsolete($"Use {nameof(IsSupporter)} instead.")]
    [JsonPropertyName("is_supporter?")]
    public bool _IsSupporter { get; set; }

    /// <summary>
    /// User's full email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    /// <summary>
    /// URL of the user's avatar on the website.
    /// </summary>
    [JsonPropertyName("profile_url")]
    public Uri? ProfileUrl { get; set; }

    /// <summary>
    /// Returns true if this member is a 'supporter' of Nexus.<br/>
    /// Supporters are users that meet the following criteria:  <br/>
    /// - Don't use an ad blocker or<br/>
    /// - Have previously in the past bought premium.  <br/>
    /// </summary>
    [JsonPropertyName("is_supporter")]
    public bool IsSupporter { get; set; }

    /// <summary>
    /// True if this user is currently a premium subscriber.
    /// </summary>
    [JsonPropertyName("is_premium")]
    public bool IsPremium { get; set; }

    /// <inheritdoc />
    public static JsonTypeInfo<ValidateInfo> GetTypeInfo() => ValidateInfoContext.Default.ValidateInfo;
}

/// <summary/>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ValidateInfo))]
public partial class ValidateInfoContext : JsonSerializerContext { }
