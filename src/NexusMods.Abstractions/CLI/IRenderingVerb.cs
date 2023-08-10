namespace NexusMods.Abstractions.CLI;

/// <summary>
/// The definition of a verb that requires a renderer.
/// </summary>
public interface IRenderingVerb : IVerb
{
    /// <summary>
    /// The renderer, that will be set by the configurator
    /// </summary>
    public IRenderer Renderer { get; set; }
}
