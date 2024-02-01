using FluentAssertions;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.Sifu.Tests;

public class SifuModInstallerTests : AModInstallerTest<Sifu, SifuModInstaller>
{
    public SifuModInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Fact]
    public async Task PicksTheRightBasePath()
    {
        var testFiles = new Dictionary<RelativePath, byte[]>();
        testFiles["foo/foo.pak"] = Array.Empty<byte>();
        testFiles["foo/foo.txt"] = Array.Empty<byte>();
        testFiles["ignored"] = Array.Empty<byte>();
        testFiles["bar/alsoignored.txt"] = Array.Empty<byte>();

        var file = await CreateTestArchive(testFiles);
        await using (file)
        {
            var (_, modFiles) = await GetModWithFilesFromInstaller(file);
            modFiles
                .Cast<IToFile>()
                .Should()
                .HaveCount(2)
                .And.AllSatisfy(x => x.To.Path.StartsWith(@"Content/Paks/~mods").Should().BeTrue())
                .And.Satisfy(
                    x => x.To.FileName == "foo.pak",
                    x => x.To.FileName == "foo.txt");
        }
    }

    [Fact]
    public async Task InstallsOnlyTheFirstSubmod()
    {
        var testFiles = new Dictionary<RelativePath, byte[]>();
        testFiles["foo/foo.pak"] = Array.Empty<byte>();
        testFiles["foo/foo.txt"] = Array.Empty<byte>();
        testFiles["bar/bar.pak"] = Array.Empty<byte>();
        testFiles["bar/bar.txt"] = Array.Empty<byte>();

        var file = await CreateTestArchive(testFiles);
        await using (file)
        {
            var (_, modFiles) = await GetModWithFilesFromInstaller(file);
            modFiles
                .Cast<IToFile>()
                .Should().HaveCount(2)
                .And.AllSatisfy(x => x.To.Path.StartsWith(@"Content/Paks/~mods").Should().BeTrue())
                .And
                .Satisfy(
                    x => x.To.Extension == new Extension(".pak"),
                    x => x.To.FileName.Extension == new Extension(".txt"));
        }
    }
}

