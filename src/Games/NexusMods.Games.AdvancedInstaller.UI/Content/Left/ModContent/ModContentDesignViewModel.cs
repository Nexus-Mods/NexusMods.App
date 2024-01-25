using System.Diagnostics.CodeAnalysis;

namespace NexusMods.Games.AdvancedInstaller.UI.ModContent;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
internal class ModContentDesignViewModel : ModContentViewModel
{
    public ModContentDesignViewModel() : base(DesignTimeHelpers.CreateDesignFileTree()) { }
}
