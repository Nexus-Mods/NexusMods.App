using System.Xml;
using System.Xml.Serialization;
using Bannerlord.LauncherManager.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.Games.MountAndBlade2Bannerlord.Extensions;
using NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager.Options;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
using NexusMods.Games.MountAndBlade2Bannerlord.Tests.Shared;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests.Utils;

public class LoadoutExtensionsTests : AGameTest<MountAndBlade2Bannerlord>
{
    public LoadoutExtensionsTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    private static LoadoutModuleViewModel ViewModelCreator(Mod mod, ModuleInfoExtendedWithPath moduleInfo, int index) => new()
    {
        Mod = mod,
        ModuleInfoExtended = moduleInfo,
        IsValid = true,
        IsSelected = mod.Enabled,
        IsDisabled = mod.Status == ModStatus.Failed,
        Index = index,
    };

    [Fact]
    public void Test()
    {
        AbsolutePath GetLauncherDataPath()
        {
            var documentsFolder = FileSystem.GetKnownPath(KnownPath.MyDocumentsDirectory);
            return documentsFolder.Combine($"{MountAndBlade2BannerlordConstants.DocumentsFolderName}/Configs/LauncherData.xml");
        }

        UserData? LoadUserData()
        {
            var path = GetLauncherDataPath();
            var xmlSerializer = new XmlSerializer(typeof(UserData));
            try
            {
                using var xmlReader = XmlReader.Create(path.Read());
                return xmlSerializer.Deserialize(xmlReader) as UserData;
            }
            catch (Exception e)
            {
                //_logger.LogError(e, "Failed to deserialize Bannerlord LauncherData file!");
                return null;
            }
        }

        string SaveUserData(UserData userData)
        {
            var path = GetLauncherDataPath();
            var xmlSerializer = new XmlSerializer(typeof(UserData));
            try
            {
                using var str = new StringWriter();
                using var xmlWriter = XmlWriter.Create(str, new XmlWriterSettings()
                {
                    Indent = true
                });
                xmlSerializer.Serialize(xmlWriter, userData);
                return str.ToString();
            }
            catch (Exception e)
            {
                //_logger.LogError(e, "Failed to serialize Bannerlord LauncherData file!");
                return "";
            }
        }

        var t = LoadUserData();
        var tt = SaveUserData(t);
    }

    [Fact]
    public async Task Test_GetViewModels()
    {
        var loadoutMarker = await CreateLoadout();

        var context = AGameTestContext.Create(CreateTestArchive, InstallModStoredFileIntoLoadout);

        await loadoutMarker.AddButterLib(context);
        await loadoutMarker.AddHarmony(context);

        var unsorted = loadoutMarker.Value.GetViewModels(ViewModelCreator).Select(x => x.Mod.Name).ToList();
        var sorted = (await loadoutMarker.Value.GetSortedViewModelsAsync(ViewModelCreator)).Select(x => x.Mod.Name).ToList();

        unsorted.Should().BeEquivalentTo(new[]
        {
            "ButterLib",
            "Harmony",
        });
        sorted.Should().BeEquivalentTo(new[]
        {
            "Harmony",
            "ButterLib",
        });
    }
}
