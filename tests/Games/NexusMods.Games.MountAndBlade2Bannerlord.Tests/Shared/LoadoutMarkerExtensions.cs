using System.Xml;
using Bannerlord.ModuleManager;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests.Shared;

public static class LoadoutMarkerExtensions
{
    public static async Task<Mod> AddNative(this LoadoutMarker loadoutMarker, AGameTestContext context)
    {
        var moduleInfo = new ModuleInfoExtended
        {
            Id = "Native",
            Name = "Native",
            Version = ApplicationVersion.TryParse("v1.0.0.0", out var bVersion) ? bVersion : ApplicationVersion.Empty,
        };

        var modFiles = moduleInfo.CreateTestFiles();
        await using var modPath = await context.CreateTestArchive(modFiles);

        return await context.InstallModStoredFileIntoLoadout(loadoutMarker, modPath, null, CancellationToken.None);
    }

    public static async Task<Mod> AddHarmony(this LoadoutMarker loadoutMarker, AGameTestContext context)
    {
        var doc = new XmlDocument();
        doc.LoadXml(Data.HarmonySubModuleXml);
        var moduleInfo = ModuleInfoExtended.FromXml(doc);

        var modFiles = moduleInfo.CreateTestFiles();
        await using var modPath = await context.CreateTestArchive(modFiles);

        return await context.InstallModStoredFileIntoLoadout(loadoutMarker, modPath, null, CancellationToken.None);
    }

    public static async Task<Mod> AddButterLib(this LoadoutMarker loadoutMarker, AGameTestContext context)
    {
        var doc = new XmlDocument();
        doc.LoadXml(Data.ButterLibSubModuleXml);
        var moduleInfo = ModuleInfoExtended.FromXml(doc);

        var modFiles = moduleInfo.CreateTestFiles();
        await using var modPath = await context.CreateTestArchive(modFiles);

        return await context.InstallModStoredFileIntoLoadout(loadoutMarker, modPath, null, CancellationToken.None);
    }

    public static async Task<Mod> AddFakeButterLib(this LoadoutMarker loadoutMarker, AGameTestContext context)
    {
        var moduleInfo = new ModuleInfoExtended
        {
            Id = "Bannerlord.ButterLib",
            Name = "ButterLib",
            Version = ApplicationVersion.TryParse("v1.0.0.0", out var bVersion) ? bVersion : ApplicationVersion.Empty,
            DependentModuleMetadatas = new []
            {
                new DependentModuleMetadata("Bannerlord.Harmony", LoadType.LoadBeforeThis, false, false, ApplicationVersion.TryParse("v3.0.0.0", out var a2Version) ? a2Version : ApplicationVersion.Empty, ApplicationVersionRange.Empty)
            }
        };
        var modFiles = moduleInfo.CreateTestFiles();
        await using var modPath = await context.CreateTestArchive(modFiles);

        return await context.InstallModStoredFileIntoLoadout(loadoutMarker, modPath, null, CancellationToken.None);
    }
}
