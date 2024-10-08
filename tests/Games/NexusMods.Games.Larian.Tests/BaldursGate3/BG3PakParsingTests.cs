using System.Text;
using FluentAssertions;
using NexusMods.Games.Larian.BaldursGate3.Utils.PakParsing;
using NexusMods.Paths;

namespace NexusMods.Games.Larian.Tests.BaldursGate3;

public class BG3PakParsingTests
{
    private readonly IFileSystem _fs;

    public BG3PakParsingTests(IFileSystem fs)
    {
        _fs = fs;
    }

    [Theory]
    [InlineData("AllItems.pak")]
    [InlineData("dnd5rbardcollegeofswordshomebr-hu6v.pak")]
    [InlineData("MoreSpellSlotsAndFeats.pak")]
    [InlineData("Waypoint Inside Emerald Grove.pak")]
    [InlineData("Carry Weight Increased 9000 - X900.pak")]
    public async Task ParsePakMeta_ShouldParseCorrectly(string pakFilePath)
    {
        var fullPath = _fs.GetKnownPath(KnownPath.EntryDirectory).Combine("BaldursGate3/Resources/PakFiles/" + pakFilePath);
        await using var pakFileStream = File.OpenRead(fullPath.ToString());
        var metaFileData = PakFileParser.ParsePakMeta(pakFileStream);
        var sb = new StringBuilder();
        sb.AppendLine("ModuleShortDesc:");
        sb.Append(LsxXmlFormat.SerializeModuleShortDesc(metaFileData.ModuleShortDesc));
        foreach (var dependency in metaFileData.Dependencies)
        {
            sb.AppendLine("Dependency:");
            sb.Append(LsxXmlFormat.SerializeModuleShortDesc(dependency));
        }
        await Verify(sb.ToString()).UseParameters(pakFilePath);
    }
    
    [Fact]
    public async Task ParsePakMeta_ShouldThrowOnBadPak()
    {
        var fullPath = _fs.GetKnownPath(KnownPath.EntryDirectory).Combine("BaldursGate3/Resources/PakFiles/malformed.pak");
        await using var pakFileStream = File.OpenRead(fullPath.ToString());
        {
            var act = () => PakFileParser.ParsePakMeta(pakFileStream);
            act.Should().Throw<InvalidDataException>();
        }
        
    }
}
