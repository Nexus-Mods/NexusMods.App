using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions;
using NexusMods.Abstractions.Games.ArchiveMetadata;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.CLI.Types;
using NexusMods.CrossPlatform.ProtocolRegistration;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;
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
            .AddVerb(() => DownloadAndInstallMod)
            .AddVerb(() => ProtocolInvoke);


    [Verb("associate-nxm", "Associate the nxm:// protocol with this application")]
    private static Task<int> AssociateNxm([Injected] IProtocolRegistration protocolRegistration)
    {
        protocolRegistration.RegisterSelf("nxm");
        return Task.FromResult(0);
    }


    [Verb("download-and-install-mod", "Download a mod and install it in one step")]
    private static async Task<int> DownloadAndInstallMod([Injected] IRenderer renderer,
        [Option("u","url", "The url of the mod to download")] Uri uri,
        [Option("l", "loadout", "The loadout to install the mod to")] LoadoutMarker loadout,
        [Option("n", "name", "The name of the mod after installing")] string? modName,
        [Injected] IHttpDownloader httpDownloader,
        [Injected] TemporaryFileManager temporaryFileManager,
        [Injected] IEnumerable<IDownloadProtocolHandler> handlers,
        [Injected] IArchiveInstaller archiveInstaller,
        [Injected] IFileOriginRegistry fileOriginRegistry,
        [Injected] CancellationToken token)

    {
        await using var temporaryPath = temporaryFileManager.CreateFile();
        return await renderer.WithProgress(token, async () =>
        {
            var name = (modName ?? "New Mod");
            var handler = handlers.FirstOrDefault(x => x.Protocol == uri.Scheme);
            if (handler != null)
            {
                await handler.Handle(uri, loadout, name, default);
                return 0;
            }

            await httpDownloader.DownloadAsync(new[] { new HttpRequestMessage(HttpMethod.Get, uri) },
                temporaryPath, null, null, token);

            var downloadId = await fileOriginRegistry.RegisterDownload(temporaryPath,
                new FilePathMetadata
                {
                    OriginalName = temporaryPath.Path.Name,
                    Quality = Quality.Low,
                    Name = name
                }, token);
            await archiveInstaller.AddMods(loadout.Value.LoadoutId, downloadId,
                string.IsNullOrWhiteSpace(modName) ? null : modName, token: token);
            return 0;
        });
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
