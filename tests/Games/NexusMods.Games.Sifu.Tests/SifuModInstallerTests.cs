using System.Text;
using FluentAssertions;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

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
            var filesToExtract = await GetFilesToExtractFromInstaller(file.Path);
            filesToExtract.Should().HaveCount(2);
            filesToExtract.Should().AllSatisfy(x => x.To.Path.StartsWith(@"Content\Paks\~mods"));
            filesToExtract.Should().Contain(x => x.To.FileName == "foo.pak");
            filesToExtract.Should().Contain(x => x.To.FileName == "foo.txt");
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
            var filesToExtract = await GetFilesToExtractFromInstaller(file.Path);
            filesToExtract.Should().HaveCount(2);
            filesToExtract.Should().AllSatisfy(x => x.To.Path.StartsWith(@"Content\Paks\~mods"));
            filesToExtract.Should().Contain(x => x.To.FileName == "foo.pak");
            filesToExtract.Should().Contain(x => x.To.FileName == "foo.txt");
        }
    }
}

