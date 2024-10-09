using NexusMods.Games.Larian.BaldursGate3.Utils.PakParsing;
using NexusMods.Paths;

namespace NexusMods.Games.Larian.Tests.BaldursGate3;

public class BG3ModsettingsFileWriterTests
{
    private readonly IFileSystem _fs;

    public BG3ModsettingsFileWriterTests(IFileSystem fs)
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
        
        // Sort the list by Name
        moduleShortDescs = moduleShortDescs.OrderBy(m => m.Name).ToList();

        var modsettingsString = ModsettingsFileWriter.SerializeModsettingsLoadOrder(moduleShortDescs);
        await Verify(modsettingsString).UseParameters(pakFolderPath);
    }
}
