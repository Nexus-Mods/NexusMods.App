using Microsoft.Extensions.DependencyInjection;
using NexusMods.ProxyConsole.RenderDefinitions;

namespace NexusMods.ProxyConsole;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to make it easier to add renderable types.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds a <see cref="IRenderableDefinition"/> to the service collection, defining how to render the given type.
    /// Each renderable type must have a unique <see cref="Guid"/> associated with it.
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddRenderable<T>(this IServiceCollection services) where T : class, IRenderableDefinition
        => services.AddSingleton<IRenderableDefinition, T>();

    /// <summary>
    /// Adds a <see cref="IRenderableDefinition"/> to the service collection for the built-in renderable types.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddDefaultRenderers(this IServiceCollection services) =>
        services.AddRenderable<TableRenderDefinition>()
        .AddRenderable<TextRenderDefinition>();
}
