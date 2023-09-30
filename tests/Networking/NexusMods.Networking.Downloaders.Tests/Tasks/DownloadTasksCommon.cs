using FluentAssertions;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Paths;
using NSubstitute;

namespace NexusMods.Networking.Downloaders.Tests.Tasks;


internal static class DownloadTasksCommon
{
    /// <summary>
    /// Creates a mock that will receive a downloaded file and assert it exists.
    /// </summary>
    public static IDownloadService CreateMockWithConfirmFileReceive()
    {
        var res = Substitute.For<IDownloadService>();
        res
            .FinalizeDownloadAsync(Arg.Any<IDownloadTask>(), Arg.Any<TemporaryPath>(), Arg.Any<string>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo =>
            {
                var task = callInfo.ArgAt<IDownloadTask>(0);
                var tempPath = callInfo.ArgAt<TemporaryPath>(1);
                var modName = callInfo.ArgAt<string>(2);

                try
                {
                    task.Should().NotBeNull();
                    tempPath.Path.FileExists.Should().BeTrue();
                    modName.Should().NotBeNullOrEmpty();

                    using var stream = tempPath.Path.Open(FileMode.Open);
                    stream.Length.Should().BePositive();
                }
                finally
                {
                    // Clean up after our test!!
                    tempPath.Dispose();
                }
            });

        return res;
    }
}
