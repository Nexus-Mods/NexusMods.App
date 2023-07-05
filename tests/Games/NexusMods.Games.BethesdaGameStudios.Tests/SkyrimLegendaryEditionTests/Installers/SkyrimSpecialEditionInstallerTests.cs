using NexusMods.Games.BethesdaGameStudios.Tests.Installers;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.Tests.SkyrimLegendaryEditionTests.Installers;

public class SkyrimLegendaryEditionInstallerTests : SkyrimInstallerTests<SkyrimLegendaryEdition>
{
    public SkyrimLegendaryEditionInstallerTests(
        IServiceProvider serviceProvider, 
        TestModDownloader downloader, 
        IFileSystem realFs) : 
        base(serviceProvider, downloader, realFs) { }
}
