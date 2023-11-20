using System.Diagnostics.CodeAnalysis;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.ModContent;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
internal class ModContentDesignViewModel : ModContentViewModel
{
    public ModContentDesignViewModel() : base(DesignTimeHelpers.CreateDesignFileTree()) { }

}
