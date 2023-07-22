using FluentAssertions;
using Moq;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using NexusMods.Paths;

namespace NexusMods.UI.Tests.RightContent.Downloads.ViewModels;

public class DownloadTaskViewModelTests
{
    [Fact]
    public void Name_ShouldReturnCorrectValue()
    {
        // Arrange
        var mockTask = new Mock<IDownloadTask>();
        mockTask.Setup(t => t.FriendlyName).Returns("TestName");
        var viewModel = new DownloadTaskViewModel(mockTask.Object);

        // Act
        var result = viewModel.Name;

        // Assert
        Assert.Equal("TestName", result);
    }

    [Fact]
    public void Version_ShouldReturnCorrectValue_WhenTaskImplementsIHaveDownloadVersion()
    {
        // Arrange
        var mockTask = new Mock<IDownloadTask>();
        mockTask.As<IHaveDownloadVersion>().Setup(t => t.Version).Returns("1.0");
        var viewModel = new DownloadTaskViewModel(mockTask.Object);

        // Act
        var result = viewModel.Version;

        // Assert
        Assert.Equal("1.0", result);
    }

    [Fact]
    public void Version_ShouldReturnUnknown_WhenTaskDoesNotImplementIHaveDownloadVersion()
    {
        // Arrange
        var mockTask = new Mock<IDownloadTask>();
        var viewModel = new DownloadTaskViewModel(mockTask.Object);

        // Act
        var result = viewModel.Version;

        // Assert
        Assert.Equal("Unknown", result);
    }

    [Fact]
    public void Game_ShouldReturnCorrectValue_WhenTaskImplementsIHaveGameName()
    {
        // Arrange
        var mockTask = new Mock<IDownloadTask>();
        mockTask.As<IHaveGameName>().Setup(t => t.GameName).Returns("GameName");
        var viewModel = new DownloadTaskViewModel(mockTask.Object);

        // Act
        var result = viewModel.Game;

        // Assert
        Assert.Equal("GameName", result);
    }

    [Fact]
    public void Game_ShouldReturnUnknown_WhenTaskDoesNotImplementIHaveGameName()
    {
        // Arrange
        var mockTask = new Mock<IDownloadTask>();
        var viewModel = new DownloadTaskViewModel(mockTask.Object);

        // Act
        var result = viewModel.Game;

        // Assert
        Assert.Equal("Unknown", result);
    }

    [Fact]
    public void SizeBytes_ShouldReturnCorrectValue_WhenTaskImplementsIHaveFileSize()
    {
        // Arrange
        var mockTask = new Mock<IDownloadTask>();
        mockTask.As<IHaveFileSize>().Setup(t => t.SizeBytes).Returns(1000);
        var viewModel = new DownloadTaskViewModel(mockTask.Object);

        // Act
        var result = viewModel.SizeBytes;

        // Assert
        Assert.Equal(1000, result);
    }

    [Fact]
    public void SizeBytes_ShouldReturnZero_WhenTaskDoesNotImplementIHaveFileSize()
    {
        // Arrange
        var mockTask = new Mock<IDownloadTask>();
        var viewModel = new DownloadTaskViewModel(mockTask.Object);

        // Act
        var result = viewModel.SizeBytes;

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Cancel_ShouldInvokeTaskCancel()
    {
        // Arrange
        var mockTask = new Mock<IDownloadTask>();
        var viewModel = new DownloadTaskViewModel(mockTask.Object);

        // Act
        viewModel.Cancel();

        // Assert
        mockTask.Verify(t => t.Cancel(), Times.Once());
    }
    
    [Fact]
    public void Poll_ShouldRaisePropertyChanged_WhenValuesChange()
    {
        // Arrange
        var mockTask = new Mock<IDownloadTask>();
        
        // TODO: Deal with Extension Methods, for now we ignore them.
        //mockTask.Setup(t => t.DownloadJobs.GetTotalCompletion()).Returns(Size.From(5000));
        //mockTask.Setup(t => t.DownloadJobs.GetTotalThroughput(It.IsAny<IDateTimeProvider>())).Returns(Size.From(1000));
        mockTask.Setup(t => t.FriendlyName).Returns("TestName2");
        mockTask.Setup(t => t.Status).Returns(DownloadTaskStatus.Completed);
        mockTask.As<IHaveDownloadVersion>().Setup(t => t.Version).Returns("2.0");
        mockTask.As<IHaveGameName>().Setup(t => t.GameName).Returns("GameName2");
        mockTask.As<IHaveFileSize>().Setup(t => t.SizeBytes).Returns(2000);

        var viewModel = new DownloadTaskViewModel(mockTask.Object, false);
        var propertyChangedCounter = 0;
        viewModel.PropertyChanged += (sender, args) => propertyChangedCounter++;

        // Act
        viewModel.Poll();

        // Assert
        propertyChangedCounter.Should().Be(5); // Current number of properties. (-2 for extension methods)
    }
}
