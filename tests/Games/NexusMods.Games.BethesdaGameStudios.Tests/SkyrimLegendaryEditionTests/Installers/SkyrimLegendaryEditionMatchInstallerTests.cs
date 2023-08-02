using NexusMods.Games.BethesdaGameStudios.Tests.Installers;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.Tests.SkyrimLegendaryEditionTests.Installers;

public class SkyrimLegendaryEditionMatchInstallerTests : GenericFolderMatchInstallerTests<SkyrimLegendaryEdition>
{
    public SkyrimLegendaryEditionMatchInstallerTests(
        IServiceProvider serviceProvider, 
        TestModDownloader downloader, 
        IFileSystem realFs) : 
        base(serviceProvider, downloader, realFs) { }
}
