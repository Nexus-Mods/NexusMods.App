using System.Runtime.InteropServices;
using AutoFixture;
using AutoFixture.Xunit2;

namespace NexusMods.Paths.Tests.AutoData;

public class AutoFileSystemAttribute : AutoDataAttribute
{
    public AutoFileSystemAttribute() : base(() =>
    {
        var ret = new Fixture();

        ret.Customize<InMemoryFileSystem>(composer => composer
            .FromFactory(() => new InMemoryFileSystem()));
        ret.Customize<IFileSystem>(composer => composer
            .FromFactory(() => new InMemoryFileSystem()));

        ret.Customize<AbsolutePath>(composer => composer
            .FromFactory<IFileSystem, string>((fs, path)
                => fs.FromFullPath(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"C:\\{path}" : $"/{path}")));

        return ret;
    })
    { }
}
