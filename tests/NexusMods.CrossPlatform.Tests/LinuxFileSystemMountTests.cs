using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using NexusMods.CrossPlatform.Process;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform.Tests;

public class LinuxFileSystemMountTests
{
    [Fact]
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "valid on all platforms")]
    public void Test_ParseFileSystemMounts()
    {
        const string input = """
Filesystem     Type    1K-blocks       Avail Mounted on
/dev/nvme0n1p2 btrfs   974663348   865677740 /
/dev/nvme0n1p2 btrfs   974663348   865677740 /var/tmp
/dev/nvme0n1p2 btrfs   974663348   865677740 /srv
/dev/nvme0n1p2 btrfs   974663348   865677740 /var/cache
/dev/nvme0n1p2 btrfs   974663348   865677740 /root
/dev/nvme0n1p2 btrfs   974663348   865677740 /var/log
/dev/nvme0n1p2 btrfs   974663348   865677740 /home
/dev/sdc1      btrfs  3907016704  3314146976 /mnt/hdd1
/dev/sdb2      ntfs3 15625861116 15087785212 /mnt/hdd3
/dev/sda1      btrfs  3907016704   940272700 /mnt/redline
""";

        input.Should().StartWith("Filesystem").And.EndWith("/mnt/redline");

        var fs = new InMemoryFileSystem(os: OSInformation.FakeUnix);
        var output = OSInteropLinux.ParseFileSystemMounts(fs, input);
        output.Should().HaveCount(4).And.BeEquivalentTo([
            new FileSystemMount(
                Source: "/dev/nvme0n1p2",
                Target: fs.FromUnsanitizedFullPath("/"),
                Type: "btrfs",
                BytesTotal: Size.From(998055268352),
                BytesAvailable: Size.From(886454005760)
            ),
            new FileSystemMount(
                Source: "/dev/sdc1",
                Target: fs.FromUnsanitizedFullPath("/mnt/hdd1"),
                Type: "btrfs",
                BytesTotal: Size.From(4000785104896),
                BytesAvailable: Size.From(3393686503424)
            ),
            new FileSystemMount(
                Source: "/dev/sdb2",
                Target: fs.FromUnsanitizedFullPath("/mnt/hdd3"),
                Type: "ntfs3",
                BytesTotal: Size.From(16000881782784),
                BytesAvailable: Size.From(15449892057088)
            ),
            new FileSystemMount(
                Source: "/dev/sda1",
                Target: fs.FromUnsanitizedFullPath("/mnt/redline"),
                Type: "btrfs",
                BytesTotal: Size.From(4000785104896),
                BytesAvailable: Size.From(962839244800)
            ),
        ], options => options.IncludingAllDeclaredProperties());

        const string input2 = """
Filesystem
/dev/sda1
""";

        var mount = OSInteropLinux.ParseFileSystemMount(output, input2);
        mount.Should().Be(output[3]);
    }
}
