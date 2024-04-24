using FluentAssertions;
using NexusMods.App.UI.Pages.Downloads.ViewModels;
using NexusMods.Networking.Downloaders.Interfaces;
using NSubstitute;

namespace NexusMods.UI.Tests.RightContent.Downloads.ViewModels;

public class DownloadTaskViewModelTests
{
    [Fact]
    public void Name_ShouldReturnCorrectValue()
    {
        // Arrange
        var task = Substitute.For<IDownloadTask>();
        task.PersistentState.FriendlyName!.Returns("TestName");
        var viewModel = new DownloadTaskViewModel(task);

        // Act
        var result = viewModel.Name;

        // Assert
        result.Should().Be("TestName");
    }

    [Fact]
    public void Version_ShouldReturnCorrectValue_WhenTaskImplementsIHaveDownloadVersion()
    {
        // Arrange
        var task = Substitute.For<IDownloadTask, IHaveDownloadVersion>();
        (task as IHaveDownloadVersion)!.Version.Returns("1.0");
        var viewModel = new DownloadTaskViewModel(task);

        // Act
        var result = viewModel.Version;

        // Assert
        result.Should().Be("1.0");
    }

    [Fact]
    public void Version_ShouldReturnUnknown_WhenTaskDoesNotImplementIHaveDownloadVersion()
    {
        // Arrange
        var task = Substitute.For<IDownloadTask>();
        var viewModel = new DownloadTaskViewModel(task);

        // Act
        var result = viewModel.Version;

        // Assert
        result.Should().Be("Unknown");
    }

    [Fact]
    public void Game_ShouldReturnCorrectValue_WhenTaskImplementsIHaveGameName()
    {
        // Arrange
        var task = Substitute.For<IDownloadTask, IHaveGameName>();
        (task as IHaveGameName)!.GameName.Returns("GameName");
        var viewModel = new DownloadTaskViewModel(task);

        // Act
        var result = viewModel.Game;

        // Assert
        result.Should().Be("GameName");
    }

    [Fact]
    public void Game_ShouldReturnUnknown_WhenTaskDoesNotImplementIHaveGameName()
    {
        // Arrange
        var task = Substitute.For<IDownloadTask>();
        var viewModel = new DownloadTaskViewModel(task);

        // Act
        var result = viewModel.Game;

        // Assert
        result.Should().Be("Unknown");
    }

    [Fact]
    public void SizeBytes_ShouldReturnCorrectValue_WhenTaskImplementsIHaveFileSize()
    {
        // Arrange
        var task = Substitute.For<IDownloadTask, IHaveFileSize>();
        (task as IHaveFileSize)!.SizeBytes.Returns(1000);
        var viewModel = new DownloadTaskViewModel(task);

        // Act
        var result = viewModel.SizeBytes;

        // Assert
        result.Should().Be(1000);
    }

    [Fact]
    public void SizeBytes_ShouldReturnZero_WhenTaskDoesNotImplementIHaveFileSize()
    {
        // Arrange
        var task = Substitute.For<IDownloadTask>();
        var viewModel = new DownloadTaskViewModel(task);

        // Act
        var result = viewModel.SizeBytes;

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Cancel_ShouldInvokeTaskCancel()
    {
        // Arrange
        var task = Substitute.For<IDownloadTask>();
        var viewModel = new DownloadTaskViewModel(task);

        // Act
        viewModel.Cancel();

        // Assert
        task.Received(1).Cancel();
    }

    [Fact]
    public void Poll_ShouldRaisePropertyChanged_WhenValuesChange()
    {
        // Arrange
        var task = (IDownloadTask)Substitute.For(new[]
        {
            typeof(IDownloadTask),
            typeof(IHaveDownloadVersion),
            typeof(IHaveGameName),
            typeof(IHaveFileSize)
        }, Array.Empty<object>());

        // TODO: Deal with Extension Methods, for now we ignore them.
        //mockTask.Setup(t => t.DownloadJobs.GetTotalCompletion()).Returns(Size.From(5000));
        //mockTask.Setup(t => t.DownloadJobs.GetTotalThroughput(It.IsAny<IDateTimeProvider>())).Returns(Size.From(1000));
        task.FriendlyName.Returns("TestName2");
        task.Status.Returns(DownloadTaskStatus.Downloading);
        (task as IHaveDownloadVersion)!.Version.Returns("2.0");
        (task as IHaveGameName)!.GameName.Returns("GameName2");
        (task as IHaveFileSize)!.SizeBytes.Returns(2000);

        var viewModel = new DownloadTaskViewModel(task, false);
        var propertyChangedCounter = 0;
        viewModel.PropertyChanged += (sender, args) => propertyChangedCounter++;

        task.FriendlyName.Returns("TestName3");
        task.Status.Returns(DownloadTaskStatus.Completed);
        (task as IHaveDownloadVersion)!.Version.Returns("3.0");
        (task as IHaveGameName)!.GameName.Returns("GameName3");
        (task as IHaveFileSize)!.SizeBytes.Returns(3000);

        // Act
        viewModel.Poll();

        // Assert
        propertyChangedCounter.Should().Be(5); // Current number of properties. (-2 for extension methods)
    }
}
