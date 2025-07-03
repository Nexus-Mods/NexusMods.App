using JetBrains.Annotations;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// Documentation for a collection of verbs
/// </summary>
[PublicAPI]
public record ModuleDefinition(string Name, string Description);
