using JetBrains.Annotations;
using MemoryPack;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// A thing that can be rendered to the console.
/// </summary>
[MemoryPackable(GenerateType.NoGenerate)]
public partial interface IRenderable;

/// <summary>
/// Generic version of <see cref="IRenderable"/>.
/// </summary>
[MemoryPackable(GenerateType.NoGenerate)]
public partial interface IRenderable<TSelf> : IRenderable where TSelf : IMemoryPackable<TSelf>;
