using System.Net;

namespace NexusMods.Abstractions.NexusWebApi.DTOs;

/// <summary>
/// Represents an individual response from a Nexus API request; containing the received raw data
/// and any useful metadata as an accessible object.
/// </summary>
/// <typeparam name="T"></typeparam>
public class Response<T>
{
    /// <summary>
    /// The data contained within the actual response, i.e. one of the members in <see cref="DTOs"/>.
    /// </summary>
    public required T Data { get; init; }

    /// <summary>
    /// Contains useful metadata sourced from the response header.
    /// </summary>
    public required ResponseMetadata Metadata { get; init; }

    /// <summary>
    /// Returned HTTP Status Code.
    /// </summary>
    public required HttpStatusCode StatusCode { get; init; }
}
