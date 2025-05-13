using JetBrains.Annotations;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Presigned upload url.
/// </summary>
[PublicAPI]
public record struct PresignedUploadUrl(Uri UploadUri, string UUID);
