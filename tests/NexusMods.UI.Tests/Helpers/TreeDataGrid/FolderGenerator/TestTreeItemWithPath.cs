
using NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.Games;

namespace NexusMods.UI.Tests.Helpers.TreeDataGrid.FolderGenerator;

/// <summary>
/// Dummy implementation for testing purposes
/// </summary>
public class TestTreeItemWithPath : ITreeItemWithPath
{
    // Store the components needed to create a GamePath
    public required LocationId LocationId { get; init; }
    public required RelativePath RelativePath { get; init; }

    // Implement the interface method
    public GamePath GetPath() => new(LocationId, RelativePath);

    // Keep Key and Name for convenience in test setup, although not part of the interface
    public required GamePath Key { get; init; }
    public required string Name { get; init; }
}
