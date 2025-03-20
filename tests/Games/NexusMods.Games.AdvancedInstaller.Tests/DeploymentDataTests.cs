using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Games.AdvancedInstaller.Exceptions;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.Tests;

public class DeploymentDataTests
{
    // Note: Paths use / as separator on all platforms to ensure conformance with our backend decisions.
    [Fact]
    public void AddMapping_Should_AddCorrectMapping()
    {
        // Arrange
        var data = new DeploymentData();
        RelativePath archivePath = "archive/file1";
        var outputPath = new GamePath(LocationId.Game, "Data/file1");

        // Act
        data.AddMapping(archivePath, outputPath);

        // Assert
        data.ArchiveToOutputMap.Should().ContainKey(archivePath);
        data.ArchiveToOutputMap[archivePath].Should().Be(outputPath);

        data.OutputToArchiveMap.Should().ContainKey(outputPath);
        data.OutputToArchiveMap[outputPath].Should().Be(archivePath);
    }

    [Fact]
    public void RemoveMapping_Should_RemoveCorrectMapping()
    {
        // Arrange
        var data = new DeploymentData();
        RelativePath archivePath = "archive/file1";
        var outputPath = new GamePath(LocationId.Game, "Data/file1");
        data.AddMapping(archivePath, outputPath);

        // Act
        var result = data.RemoveMapping(archivePath);

        // Assert
        result.Should().BeTrue();
        data.ArchiveToOutputMap.Should().NotContainKey(archivePath);
        data.OutputToArchiveMap.Should().NotContainKey(outputPath);
    }

    [Fact]
    public void ClearMappings_Should_ClearAllMappings()
    {
        // Arrange
        var data = new DeploymentData();
        data.AddMapping("archive/file1", new GamePath(LocationId.Game, "Data/file1"));
        data.AddMapping("archive/file2", new GamePath(LocationId.Game, "Data/file2"));

        // Act
        data.ClearMappings();

        // Assert
        data.ArchiveToOutputMap.Should().BeEmpty();
        data.OutputToArchiveMap.Should().BeEmpty();
    }

    [Fact]
    public void AddMapping_WithDuplicateOutputPath_Should_ThrowMappingAlreadyExists()
    {
        // Arrange
        var data = new DeploymentData();
        RelativePath archivePath1 = "archive/file1";
        RelativePath archivePath2 = "archive/file2";
        var outputPath = new GamePath(LocationId.Game, "Data/file1");

        // Act
        data.AddMapping(archivePath1, outputPath);

        // Assert
        var act = () => data.AddMapping(archivePath2, outputPath);
        act.Should().Throw<MappingAlreadyExistsException>();
    }

    [Fact]
    public void AddMapping_WithDuplicateOutputPath_AndForced_ShouldRemap()
    {
        // Arrange
        var data = new DeploymentData();
        RelativePath archivePath1 = "archive/file1";
        RelativePath archivePath2 = "archive/file2";
        var outputPath = new GamePath(LocationId.Game, "Data/file1");

        // Act
        data.AddMapping(archivePath1, outputPath);

        // Assert
        data.AddMapping(archivePath2, outputPath, true);

        data.ArchiveToOutputMap.Should().ContainKey(archivePath2);
        data.ArchiveToOutputMap.Should().NotContainKey(archivePath1);
        data.ArchiveToOutputMap[archivePath2].Should().Be(outputPath);

        data.OutputToArchiveMap.Should().ContainKey(outputPath);
        data.OutputToArchiveMap[outputPath].Should().Be(archivePath2);
    }
}
