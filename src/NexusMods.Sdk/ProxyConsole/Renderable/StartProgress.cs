using JetBrains.Annotations;
using MemoryPack;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// The start or end of a "WithProgress" block
/// </summary>
[MemoryPackable]
[PublicAPI]
public partial class StartProgress : IRenderable<StartProgress>;
