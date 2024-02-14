# Installing Mods

!!! info "Informs you how you can add support for installing different kinds of mods."

!!! tip "What does 'Installing a Mod' mean in the context of the Nexus Mods App?"

    The *Nexus Mods App* is strictly a *Mod Manager*, ***not*** a mod loader/framework (e.g. it's not ASI Loader, SMAPI, UE4SS, BepinEx, MelonLoader etc.).

    In the context of the *Nexus Mods App*, 'Installing Mod' means 'Decide where to put it files belonging to a mod in the Mod Folder'
    for an existing 'loader' or 'game' to use.

Mod installation in the app happens in several phases:

1. The user selects a mod to install.
2. The app extracts the archive.
3. The app feeds the extracted files to all `IModInstaller`(s) associated with the game via `AGame.Installers`.
    - [Related Documentation: Adding A Game](./AddingAGame.md).
4. The [Mod Installer](#an-example-installer-imodinstaller) returns 1 or more `ModInstallerResult`(s).
    - These result objects tell the app where to place the files.
    - Returns more than one result if multiple mods were present in an archive.
    - You can return metadata in these objects, which can be reused in future runs of your installer (e.g. remember settings).

When this process is done, the App compresses the downloaded mods into [Nx Archives][nx-archive] for future
extraction to game folder.

## An Example Installer (IModInstaller)

```csharp
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
public class SifuModInstaller : AModInstaller
{
    private static readonly Extension PakExt = new(".pak");
    private static readonly RelativePath ModsPath = "Content/Paks/~mods".ToRelativePath();

    public SifuModInstaller(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        // Find a PAK file
        var pakFile = info.ArchiveFiles.GetFiles()
            .FirstOrDefault(node => node.Path().Extension == PakExt);

        if (pakFile == null)
            return NoResults;

        // Get folder for PAK file.
        var pakPath = pakFile.Parent();
        var modFiles = pakPath!.GetFiles()
            .Select(kv => kv.ToStoredFile(
                new GamePath(LocationId.Game, ModsPath.Join(kv.Path().RelativeTo(pakPath!.Path())))
            ));

        return new [] { new ModInstallerResult
        {
            Id = info.BaseModId,
            Files = modFiles,
            Name = pakPath!.FileName()
        }};
    }
}
```

This installer for Unreal Engine 4 game, [Sifu][sifu] will:

- Find folder containing a `.pak` file.
- Extract all files in that folder to `Content/Paks/~mods`.

!!! tip "Once you implement your installer, don't forget to add it to your game."

```csharp
// 👇 in your AGame Derivative
public class Sifu : AGame, ISteamGame, IEpicGame
{
    // Right here! 🫰
    protected override IEnumerable<IModInstaller> MakeInstallers(IServiceProvider provider) => new[]
    {
        new SifuModInstaller(provider)
    };
}
```

[nx-archive]: https://nexus-mods.github.io/NexusMods.Archives.Nx/
[sifu]: https://www.nexusmods.com/sifu
