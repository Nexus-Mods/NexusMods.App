using NexusMods.Games.BethesdaGameStudios.Tests.Installers;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.Tests.Fallout4Tests.Installers;

public class Fallout4MatchInstallerTests : GenericFolderMatchInstallerTests<Fallout4.Fallout4>
{
    public Fallout4MatchInstallerTests(
        IServiceProvider serviceProvider,
        TestModDownloader downloader,
        IFileSystem realFs) : base(serviceProvider, downloader, realFs) { }
}
