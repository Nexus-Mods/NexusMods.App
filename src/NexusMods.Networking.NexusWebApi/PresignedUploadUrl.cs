using JetBrains.Annotations;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Presigned upload url.
/// </summary>
[PublicAPI]
public record struct PresignedUploadUrl(Uri UploadUri, string UUID)
{
    /// <summary>
    /// Creates <see cref="PresignedUploadUrl"/> from API return value <see cref="IPresignedUrl"/>.
    /// </summary>
    public static PresignedUploadUrl FromApi(IPresignedUrl data) => new(new Uri(data.Url), data.Uuid);

    /// <summary/>
    public static implicit operator Uri(PresignedUploadUrl self) => self.UploadUri;
}
