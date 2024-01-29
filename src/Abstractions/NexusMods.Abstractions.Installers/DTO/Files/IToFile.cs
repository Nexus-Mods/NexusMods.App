using NexusMods.Abstractions.GameLocators;

namespace NexusMods.Abstractions.Installers.DTO.Files;

/// <summary>
/// A mod file that will be installed to the game folders in the <see cref="To"/> path.
/// </summary>
public interface IToFile
{
    /// <summary>
    /// The destination path of the mod file.
    /// </summary>
    public GamePath To { get; }
}
