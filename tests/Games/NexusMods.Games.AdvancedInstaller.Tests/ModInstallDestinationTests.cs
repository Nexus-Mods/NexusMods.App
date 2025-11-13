using FluentAssertions;

using NexusMods.Paths;
using NexusMods.Sdk.Games;

namespace NexusMods.Games.AdvancedInstaller.Tests;

/// <summary>
/// Tests for ModInstallDestination and friends.
/// </summary>
public class ModInstallDestinationTests
{
    [Fact]
    public void FromInstallFolderTargets_WithoutChild()
    {
        // Arrange
        var targets = new List<InstallFolderTarget> { DataInstallFolderTarget };

        // Act
        var result = ModInstallDestinationHelpers.FromInstallFolderTargets(targets);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(targets.Count);
        for (var i = 0; i < result.Count; i++)
        {
            result[i].DestinationGamePath.Should().Be(targets[i].DestinationGamePath);
        }
    }

    [Fact]
    public void FromInstallFolderTargets_DetectsNestedChildren()
    {
        // Arrange
        var targets = new List<InstallFolderTarget> { GameRootInstallFolderTarget };

        // Act
        var result = ModInstallDestinationHelpers.FromInstallFolderTargets(targets);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    static readonly InstallFolderTarget DataInstallFolderTarget = new()
    {
        DestinationGamePath = new GamePath(LocationId.Game, "Data"),
        KnownValidFileExtensions = new[]
        {
            new Extension(".esp"),
            new Extension(".esm"),
            new Extension(".esl"),
        }
    };

    static readonly InstallFolderTarget GameRootInstallFolderTarget = new()
    {
        DestinationGamePath = new GamePath(LocationId.Game, RelativePath.Empty),
        Names = [ "data" ],
        SubTargets = [DataInstallFolderTarget]
    };
}
