using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.SingleProcess;

/// <summary>
/// A console that exists on the client side of the proxy, calls to .Console will be rendered on the client,
/// and input on the client will show up on the IAnsiConsoleInput in the .Console property
/// </summary>
public class ConsoleSettings
{
    /// <summary>
    /// The arguments passed to the client application instance
    /// </summary>
    public required string[] Arguments { get; init; }

    /// <summary>
    /// The console to use for interaction with the application instance
    /// </summary>
    public required IRenderer Renderer { get; init; }

}
