using System.Diagnostics.CodeAnalysis;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.ModContent;

/// <summary>
///     Design ViewModel for root node.
/// </summary>
[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
internal class ModContentTreeEntryDesignViewModel : ModContentTreeEntryViewModel
{
    public ModContentTreeEntryDesignViewModel() : base("", true) { }
}
