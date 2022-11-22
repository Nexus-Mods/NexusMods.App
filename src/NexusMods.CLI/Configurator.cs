using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace NexusMods.CLI;

public class Configurator
{
    private readonly IEnumerable<IRenderer> _renderers;
    public IRenderer Renderer { get; private set; }
    public Configurator(IEnumerable<IRenderer> renderers)
    {
        _renderers = renderers;
    }

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