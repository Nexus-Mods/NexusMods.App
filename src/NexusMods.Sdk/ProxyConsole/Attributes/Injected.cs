using JetBrains.Annotations;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// Marks a parameter as an injected dependency.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[PublicAPI]
public class InjectedAttribute : Attribute;
