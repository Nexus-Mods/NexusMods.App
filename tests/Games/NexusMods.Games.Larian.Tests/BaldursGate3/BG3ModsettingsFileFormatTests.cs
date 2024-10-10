using System.Text;
using NexusMods.Games.Larian.BaldursGate3.Utils.PakParsing;
using NexusMods.Paths;

namespace NexusMods.Games.Larian.Tests.BaldursGate3;

public class BG3ModsettingsFileFormatTests
{
    private readonly IFileSystem _fs;

    public BG3ModsettingsFileFormatTests(IFileSystem fs)
    {
        _fs = fs;
    }
    
    [Fact]
    public async Task SerializeModsettingsLoadOrder_VerifyTest()
    {
        var pakFolderPath = _fs.GetKnownPath(KnownPath.EntryDirectory).Combine("BaldursGate3/Resources/PakFiles/");
        var pakFiles = Directory.GetFiles(pakFolderPath.ToString(), "*.pak");

        var moduleShortDescs = new List<LsxXmlFormat.ModuleShortDesc>();

        foreach (var pakFilePath in pakFiles)
        {
            await using var pakFileStream = File.OpenRead(pakFilePath);
            LsxXmlFormat.MetaFileData metaFileData;
            try
            {
                metaFileData = PakFileParser.ParsePakMeta(pakFileStream);
            }
            catch (InvalidDataException)
            {
                // Skip malformed pak test files
                continue;
            }
            moduleShortDescs.Add(metaFileData.ModuleShortDesc);
        }
        
        // Sort the list by Uuid to simulate the load order
        var loadOrder = moduleShortDescs.OrderBy(m => m.Uuid).ToArray();

        var modsettingsString = ModsettingsFileFormat.SerializeModsettingsLoadOrder(loadOrder);
        await Verify(modsettingsString).UseParameters(pakFolderPath);
    }
    
    [Theory]
    [InlineData("example-modsettings-Patch7-Hotfix27.lsx")]
    
    public async Task DeserializeModsettingsLoadOrder_VerifyTest(string modsettingsFileName)
    {
        var modsettingsFilePath = _fs.GetKnownPath(KnownPath.EntryDirectory).Combine("BaldursGate3/Resources/ModsettingsFiles/" + modsettingsFileName);
        var modsettingsXml = await File.ReadAllTextAsync(modsettingsFilePath.ToString());
        
        // Deserialize the modsettings file
        var moduleShortDescs = ModsettingsFileFormat.DeserializeModsettingsLoadOrder(modsettingsXml);
        
        // re-serialize the moduleShortDescs
        var modsettingsString = ModsettingsFileFormat.SerializeModsettingsLoadOrder(moduleShortDescs);
        // Verify the re-serialized string
        await Verify(modsettingsString).UseParameters(modsettingsFileName);}
}
