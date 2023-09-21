using System.Runtime.InteropServices;
using FluentAssertions;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests;

public class GameLocationsRegisterTests
{
    [Theory]
    [InlineData("Game", "/foo/bar/Skyrim", true)]
    [InlineData("Data", "/foo/bar/Skyrim/data", true)]
    [InlineData("SKSE Plugins", "/foo/bar/Skyrim/data/skse/plugins", true)]
    [InlineData("Saves", "/foo/baz/documents/My Games/Skyrim/Saves", true)]
    [InlineData("Preferences", "/foo/baz/documents/My Games/Skyrim", true)]
    [InlineData("Documents", "/foo/baz/documents/My Games/Skyrim", true)]
    [InlineData("AppData", "/foo/qux/AppData/Local/Skyrim", true)]
    [InlineData("Unknown", "", false)]
    [InlineData("game", "", false)]
    public void IndexTest(GameFolderType id, string expected, bool found)
    {
        var locationsRegister = new GameLocationsRegister(CreateTestLocations());
        if (found)
        {
            locationsRegister[id].Should().Be(CreateAbsPath(expected));
        }
        else
        {
            locationsRegister.Invoking(x => x[id]).Should().Throw<KeyNotFoundException>();
        }
    }

    [Fact]
    public void GetTopLevelLocationsTest()
    {
        var locationsRegister = new GameLocationsRegister(CreateTestLocations());
        locationsRegister.GetTopLevelLocations().Should().BeEquivalentTo(
            new KeyValuePair<GameFolderType, AbsolutePath>[]
            {
                new(GameFolderType.Game, CreateAbsPath("/foo/bar/Skyrim")),
                new(GameFolderType.Preferences, CreateAbsPath("/foo/baz/documents/My Games/Skyrim")),
                new(GameFolderType.AppData, CreateAbsPath("/foo/qux/AppData/Local/Skyrim")),
            });
    }

    [Theory]
    [InlineData("Game", true)]
    [InlineData("Data", false)]
    [InlineData("SKSE Plugins", false)]
    [InlineData("Saves", false)]
    [InlineData("Preferences", true)]
    [InlineData("Documents", true)]
    [InlineData("AppData", true)]
    public void IsTopLevelTest(GameFolderType id, bool expected)
    {
        var locationsRegister = new GameLocationsRegister(CreateTestLocations());
        locationsRegister.IsTopLevel(id).Should().Be(expected);
    }

    [Theory]
    [InlineData("Game", "Game")]
    [InlineData("Data", "Game")]
    [InlineData("SKSE Plugins", "Game")]
    [InlineData("Saves", "Preferences")]
    [InlineData("Preferences", "Preferences")]
    [InlineData("Documents", "Documents")]
    [InlineData("AppData", "AppData")]
    public void GetTopLevelParentTest(GameFolderType id, GameFolderType expected)
    {
        var locationsRegister = new GameLocationsRegister(CreateTestLocations());
        locationsRegister.GetTopLevelParent(id).Should().Be(expected);
    }

    [Theory]
    [InlineData("Game", "", "/foo/bar/Skyrim")]
    [InlineData("Game", "foo", "/foo/bar/Skyrim/foo")]
    [InlineData("Data", "", "/foo/bar/Skyrim/data")]
    [InlineData("Data", "foo/bar", "/foo/bar/Skyrim/data/foo/bar")]
    [InlineData("SKSE Plugins", "", "/foo/bar/Skyrim/data/skse/plugins")]
    [InlineData("SKSE Plugins", "foo/bar/qux", "/foo/bar/Skyrim/data/skse/plugins/foo/bar/qux")]
    [InlineData("Saves", "", "/foo/baz/documents/My Games/Skyrim/Saves")]
    [InlineData("Saves", "foo/bar", "/foo/baz/documents/My Games/Skyrim/Saves/foo/bar")]
    [InlineData("Preferences", "", "/foo/baz/documents/My Games/Skyrim")]
    [InlineData("Preferences", "foo/bar", "/foo/baz/documents/My Games/Skyrim/foo/bar")]
    [InlineData("Documents", "", "/foo/baz/documents/My Games/Skyrim")]
    [InlineData("Documents", "foo/bar", "/foo/baz/documents/My Games/Skyrim/foo/bar")]
    [InlineData("AppData", "", "/foo/qux/AppData/Local/Skyrim")]
    [InlineData("AppData", "foo/bar", "/foo/qux/AppData/Local/Skyrim/foo/bar")]
    public void GetResolvedPathTest(GameFolderType id, string relativePath, string expected)
    {
        var locationsRegister = new GameLocationsRegister(CreateTestLocations());
        locationsRegister.GetResolvedPath(new GamePath(id, (RelativePath)relativePath)).Should()
            .Be(CreateAbsPath(expected));
    }

    [Theory]
    [InlineData("Game", new string[] { "Data", "SKSE Plugins" })]
    [InlineData("Data", new string[] { "SKSE Plugins" })]
    [InlineData("SKSE Plugins", new string[] { })]
    [InlineData("Saves", new string[] { })]
    [InlineData("Preferences", new string[] { "Saves" })]
    [InlineData("Documents", new string[] { "Saves" })]
    [InlineData("AppData", new string[] { })]
    public void GetNestedLocationsTest(GameFolderType id, string[] expectedChildren)
    {
        var locationsRegister = new GameLocationsRegister(CreateTestLocations());
        locationsRegister.GetNestedLocations(id).Should()
            .BeEquivalentTo(expectedChildren.Select(GameFolderType.From));
    }

    [Theory]
    [InlineData("/foo/bar/Skyrim", "Game", "")]
    [InlineData("/foo/bar/Skyrim/foo", "Game", "foo")]
    [InlineData("/foo/bar/Skyrim/data", "Data", "")]
    [InlineData("/foo/bar/Skyrim/data/foo/bar", "Data", "foo/bar")]
    [InlineData("/foo/bar/Skyrim/data/skse/plugins", "SKSE Plugins", "")]
    [InlineData("/foo/bar/Skyrim/data/skse/plugins/foo/bar/qux", "SKSE Plugins", "foo/bar/qux")]
    [InlineData("/foo/baz/documents/My Games/Skyrim/Saves", "Saves", "")]
    [InlineData("/foo/baz/documents/My Games/Skyrim/Saves/foo/bar", "Saves", "foo/bar")]
    [InlineData("/foo/baz/documents/My Games/Skyrim", "Preferences", "")]
    [InlineData("/foo/baz/documents/My Games/Skyrim/foo/bar", "Preferences", "foo/bar")]
    [InlineData("/foo/qux/AppData/Local/Skyrim", "AppData", "")]
    [InlineData("/foo/qux/AppData/Local/Skyrim/foo/bar", "AppData", "foo/bar")]
    public void ToGamePathTest(string absolutePath, GameFolderType expectedFolderType, string expectedRelativePath)
    {
        var locationsRegister = new GameLocationsRegister(CreateTestLocations());
        locationsRegister.ToGamePath(CreateAbsPath(absolutePath)).Should()
            .Be(new GamePath(expectedFolderType, (RelativePath)expectedRelativePath));
    }

    private Dictionary<GameFolderType, AbsolutePath> CreateTestLocations()
    {
        return new Dictionary<GameFolderType, AbsolutePath>()
        {
            { GameFolderType.Game, CreateAbsPath("/foo/bar/Skyrim") },
            { GameFolderType.From("Data"), CreateAbsPath("/foo/bar/Skyrim/data") },
            { GameFolderType.From("SKSE Plugins"), CreateAbsPath("/foo/bar/Skyrim/data/skse/plugins") },
            { GameFolderType.Saves, CreateAbsPath("/foo/baz/documents/My Games/Skyrim/Saves") },
            { GameFolderType.Preferences, CreateAbsPath("/foo/baz/documents/My Games/Skyrim") },
            { GameFolderType.From("Documents"), CreateAbsPath("/foo/baz/documents/My Games/Skyrim") },
            { GameFolderType.AppData, CreateAbsPath("/foo/qux/AppData/Local/Skyrim") },
        };
    }

    private static AbsolutePath CreateAbsPath(string input, bool isUnix = true)
    {
        var os = CreateOSInformation(isUnix);
        var fs = new InMemoryFileSystem(os);
        var path = fs.FromUnsanitizedFullPath(input);
        return path;
    }

    private static IOSInformation CreateOSInformation(bool isUnix)
    {
        return isUnix ? new OSInformation(OSPlatform.Linux) : new OSInformation(OSPlatform.Windows);
    }
}
