using JetBrains.Annotations;
using Vogen;

namespace NexusMods.Common.GuidedInstaller.ValueObjects;

// TODO: convert this into a discriminated union using OneOf

/// <summary>
/// URL of an asset that's tied to an option. This can be a relative path, or
/// a URL to a remote resource.
/// </summary>
[PublicAPI]
[ValueObject<string>]
public readonly partial struct AssetUrl { }
