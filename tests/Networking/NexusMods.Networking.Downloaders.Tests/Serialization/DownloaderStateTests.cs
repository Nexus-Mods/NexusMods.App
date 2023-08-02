using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using NexusMods.Networking.Downloaders.Tasks.State;

namespace NexusMods.Networking.Downloaders.Tests.Serialization;

public class DownloaderStateTests
{
    private readonly IDataStore _store;

    public DownloaderStateTests(IDataStore store) => _store = store;

    /// <summary>
    /// Tests serialization of just the base (required) members.
    /// </summary>
    [Theory]
    [AutoData]
    public void SerializeBase(string friendlyName, string downloadPath, int state)
    {
        // Arrange
        var mainInterface = new Mock<IDownloadTask>();
        mainInterface.Setup(x => x.FriendlyName).Returns(friendlyName);
        
        // Act
        var item = DownloaderState.Create(mainInterface.Object, new MockDownloaderState(state), downloadPath);

        item.EnsurePersisted(_store);
        var deserialized = _store.Get<DownloaderState>(item.DataStoreId);
        
        // Assert
        deserialized.Should().Be(item);
    }
    
    [Theory]
    [AutoData]
    public void SerializeWithGameName(string friendlyName, string downloadPath, int state, string gameName)
    {
        // Arrange
        var mainInterface = new Mock<IDownloadTask>();
        mainInterface.Setup(x => x.FriendlyName).Returns(friendlyName);

        var gameNameInterface = mainInterface.As<IHaveGameName>();
        gameNameInterface.Setup(x => x.GameName).Returns(gameName);

        // Act
        var item = DownloaderState.Create(mainInterface.Object, new MockDownloaderState(state), downloadPath);

        item.EnsurePersisted(_store);
        var deserialized = _store.Get<DownloaderState>(item.DataStoreId);
    
        // Assert
        deserialized.Should().Be(item);
    }
    
    [Theory]
    [AutoData]
    public void SerializeWithDownloadVersion(string friendlyName, string downloadPath, int state, string version)
    {
        // Arrange
        var mainInterface = new Mock<IDownloadTask>();
        mainInterface.Setup(x => x.FriendlyName).Returns(friendlyName);

        var downloadVersionInterface = mainInterface.As<IHaveDownloadVersion>();
        downloadVersionInterface.Setup(x => x.Version).Returns(version);

        // Act
        var item = DownloaderState.Create(mainInterface.Object, new MockDownloaderState(state), downloadPath);

        item.EnsurePersisted(_store);
        var deserialized = _store.Get<DownloaderState>(item.DataStoreId);
    
        // Assert
        deserialized.Should().Be(item);
    }

    [Theory]
    [AutoData]
    public void SerializeWithFileSize(string friendlyName, string downloadPath, int state, long sizeBytes)
    {
        // Arrange
        var mainInterface = new Mock<IDownloadTask>();
        mainInterface.Setup(x => x.FriendlyName).Returns(friendlyName);

        var fileSizeInterface = mainInterface.As<IHaveFileSize>();
        fileSizeInterface.Setup(x => x.SizeBytes).Returns(sizeBytes);

        // Act
        var item = DownloaderState.Create(mainInterface.Object, new MockDownloaderState(state), downloadPath);

        item.EnsurePersisted(_store);
        var deserialized = _store.Get<DownloaderState>(item.DataStoreId);
    
        // Assert
        deserialized.Should().Be(item);
    }
}

// ReSharper disable once PartialTypeWithSinglePart
/// <param name="Id">Just a random integer.</param>
[JsonName("NexusMods.Networking.Downloaders.Tests.Serialization.MockDownloaderState")]
public record MockDownloaderState(int Id = 0) : ITypeSpecificState
{
    // ReSharper disable once UnusedMember.Global - Required for serialization
    public MockDownloaderState() : this(0) { }
}; 