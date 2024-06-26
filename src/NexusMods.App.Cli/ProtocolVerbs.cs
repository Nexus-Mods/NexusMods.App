using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.CLI.Types;
using NexusMods.CrossPlatform.ProtocolRegistration;
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
    private static async Task<int> AssociateNxm([Injected] IProtocolRegistration protocolRegistration)
    {
        await protocolRegistration.RegisterHandler("nxm");
        return 0;
    }

    [Verb("download-and-install-mod", "Download a mod and install it in one step")]
    private static async Task<int> DownloadAndInstallMod([Injected] IRenderer renderer,
        [Option("u","url", "The url of the mod to download")] Uri uri,
        [Option("l", "loadout", "The loadout to install the mod to")] Loadout.ReadOnly loadout,
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
                (tx, id) =>
            {
                tx.Add(id, FilePathMetadata.OriginalName, temporaryPath.Path.Name);
            }, name, token);
            await archiveInstaller.AddMods(loadout.LoadoutId, downloadId, name, token: token);
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
