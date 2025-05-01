using NexusMods.MnemonicDB.Abstractions;
namespace NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;

/// <summary>
/// Generates incrementing numbers atomically. Used to offset fake <see cref="EntityId"/>
/// instances in <see cref="TreeFolderGeneratorForLocationId{TTreeItemWithPath,TFolderModelInitializer}"/>
/// </summary>
public class IncrementingNumberGenerator
{
    private ulong _currentNumber = 0;

    /// <summary>
    /// Gets the next number in the sequence atomically.
    /// </summary>
    /// <returns>The next incremented number.</returns>
    public ulong GetNextNumber() => Interlocked.Increment(ref _currentNumber);
}
