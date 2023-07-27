using FluentAssertions;
using Moq;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Tests.Tasks;


internal static class DownloadTasksCommon
{
    /// <summary>
    /// Creates a mock that will receive a downloaded file and assert it exists.
    /// </summary>
    public static Mock<IDownloadService> CreateMockWithConfirmFileReceive()
    {
        var mock = new Mock<IDownloadService>(); //replace with the actual interface or class name that has the FinalizeDownloadAsync method

        mock.Setup(x => x.FinalizeDownloadAsync(It.IsAny<IDownloadTask>(), It.IsAny<TemporaryPath>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask) // Adjust this line as needed, for instance if your method returns a Task<TResult> then you should use Task.FromResult(result)
            .Callback((IDownloadTask task, TemporaryPath tempPath, string modName) =>
            {
                // Place your assertions here, for example:
                try
                {
                    task.Should().NotBeNull();
                    tempPath.Path.FileExists.Should().BeTrue();
                    modName.Should().NotBeNullOrEmpty();

                    using var stream = tempPath.Path.Open(FileMode.Open);
                    stream.Length.Should().BeGreaterThan(0);
                }
                finally
                {
                    // Clean up after our test!!
                    tempPath.Dispose();
                }
            });

        return mock;
    }
}
