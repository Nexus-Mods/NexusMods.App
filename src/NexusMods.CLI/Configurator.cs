using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace NexusMods.CLI;

/// <summary>
/// Due to some circular references between renderers, commandline builders and verbs, this class allows
/// for late-binding of renderers
/// </summary>
public class Configurator
{
    private readonly IEnumerable<IRenderer> _renderers;

    public IRenderer Renderer { get; private set; }

    public Configurator(IEnumerable<IRenderer> renderers)
    {
        // Note to readers: 
        // Multiple calls to register an item in a DI container; can yield IEnumerable, 
        // because (at time of writing) we're registering the Spectre renderer first; this will be the default
        // or first one.
        _renderers = renderers;
        Renderer = _renderers.First();
    }

    /// <summary>
    /// Configures the renderer output based on the commandline arguments
    /// </summary>
    /// <param name="context"></param>
    public void Configure(InvocationContext context)
    {
        var renderer = context.BindingContext.ParseResult.RootCommandResult.Children.OfType<OptionResult>()
            .Where(o => o.Option.Name == "renderer")
            .Select(o => (IRenderer)o.GetValueOrDefault()!)
            .FirstOrDefault();

        if (renderer != null)
            Renderer = renderer;
        else
            Renderer = _renderers.FirstOrDefault(r => r.Name == "console") ?? _renderers.First();

        var noBanner = context.BindingContext.ParseResult.RootCommandResult.Children.OfType<OptionResult>()
            .Where(o => o.Option.Name == "noBanner")
            .Select(o => (bool)o.GetValueOrDefault()!)
            .FirstOrDefault();

        if (!noBanner)
            Renderer.RenderBanner();
    }

}
