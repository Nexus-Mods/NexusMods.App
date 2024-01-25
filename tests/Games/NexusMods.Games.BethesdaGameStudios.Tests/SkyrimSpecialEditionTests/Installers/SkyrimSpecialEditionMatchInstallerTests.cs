using NexusMods.Games.BethesdaGameStudios.Tests.Installers;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.Tests.SkyrimSpecialEditionTests.Installers;

public class SkyrimSpecialEditionMatchInstallerTests : GenericFolderMatchInstallerTests<SkyrimSpecialEdition.SkyrimSpecialEdition>
{
    public SkyrimSpecialEditionMatchInstallerTests(
        IServiceProvider serviceProvider, 
        TestModDownloader downloader, 
        IFileSystem realFs) : 
        base(serviceProvider, downloader, realFs) { }
}
