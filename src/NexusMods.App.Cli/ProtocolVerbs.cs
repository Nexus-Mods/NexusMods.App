using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI.Types;
using NexusMods.CrossPlatform.ProtocolRegistration;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.CLI;

/// <summary>
/// CLI verbs for the protocols
/// </summary>
public static class ProtocolVerbs
{
    /// <summary>
    /// Adds the protocol verbs to the <see cref="IServiceCollection"/>
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddProtocolVerbs(this IServiceCollection services) =>
        services.AddVerb(() => AssociateNxm)
            .AddVerb(() => ProtocolInvoke);


    [Verb("associate-nxm", "Associate the nxm:// protocol with this application")]
    private static async Task<int> AssociateNxm([Injected] IProtocolRegistration protocolRegistration)
    {
        await protocolRegistration.RegisterHandler("nxm");
        return 0;
    }
    
    [Verb("protocol-invoke", "Handle a URL with custom protocol")]
    private static async Task<int> ProtocolInvoke([Injected] IRenderer renderer,
        [Option("u", "url", "The URL to handle")] Uri uri,
        [Injected] IEnumerable<IIpcProtocolHandler> handlers,
        [Injected] CancellationToken token)
    {
        var handler = handlers.FirstOrDefault(iter => iter.Protocol == uri.Scheme);
        if (handler == null)
            throw new Exception($"Unsupported protocol \"{uri.Scheme}\"");

        await handler.Handle(uri.ToString(), token);

        return 0;
    }

}
