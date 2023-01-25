using System.Net;

namespace NexusMods.Networking.NexusWebApi.DTOs;

public class Response<T>
{
    public required T Data { get; init; }
    public required ResponseMetadata Metadata { get; init; }
    public required HttpStatusCode StatusCode { get; init; }
}