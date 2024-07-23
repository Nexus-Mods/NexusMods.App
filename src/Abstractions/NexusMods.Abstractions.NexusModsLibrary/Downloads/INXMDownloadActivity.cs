using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.HttpDownloads;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// Represents an NXM download.
/// </summary>
[PublicAPI]
public interface INXMDownloadJob : IHttpDownloadJob;
