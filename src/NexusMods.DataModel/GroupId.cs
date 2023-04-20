using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.DataModel.JsonConverters;
using Vogen;

namespace NexusMods.DataModel;

/// <summary>
/// Represents a unique identifier for a mod group.
/// </summary>
[PublicAPI]
[ValueObject<Guid>(conversions: Conversions.None)]
[JsonConverter(typeof(GroupIdConverter))]
public readonly partial struct GroupId
{
    /// <summary>
    /// Creates a new <see cref="GroupId"/> from a unique <see cref="Guid"/>.
    /// </summary>
    /// <returns></returns>
    public static GroupId New() => From(Guid.NewGuid());
}
