using JetBrains.Annotations;

namespace NexusMods.Telemetry;

/// <summary>
/// Defines an event.
/// </summary>
/// <param name="Category">The event category</param>
/// <param name="Action">The event action</param>
[PublicAPI]
public record EventDefinition(string Category, string Action);
