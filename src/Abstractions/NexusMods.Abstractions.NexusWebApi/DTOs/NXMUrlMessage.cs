using NexusMods.Abstractions.NexusWebApi.Types;

namespace NexusMods.Abstractions.NexusWebApi.DTOs;

/// <summary>
/// nxm url message used in IPC. The oauth callback will spawn a new instance of NMA
/// that then needs to send the token back to the "main" process that made the request
/// </summary>
// ReSharper disable once InconsistentNaming
public readonly struct NXMUrlMessage
{
    /// <summary>
    /// the actual url
    /// </summary>
    public NXMUrl Value { get; init; }
}
