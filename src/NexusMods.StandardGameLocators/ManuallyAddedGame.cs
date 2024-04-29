using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.DataModel.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Used to store information about manually added games.
/// </summary>
public record ManuallyAddedGame : IGameLocatorResultMetadata
{
    private const string Namespace = "NexusMods.StandardGameLocators.ManuallyAddedGame";

    /// <summary>
    /// The game domain this game install belongs to.
    /// </summary>
    public static readonly GameDomainAttribute GameDomain = new(Namespace, nameof(GameDomain)) { IsIndexed = true };

    /// <summary>
    /// The version of the game.
    /// </summary>
    public static readonly StringAttribute Version = new(Namespace, nameof(Version));

    /// <summary>
    /// The path to the game install.
    /// </summary>
    public static readonly StringAttribute Path = new(Namespace, nameof(Path)) { IsIndexed = true };

    public class Model(ITransaction tx) : Entity(tx), IGameLocatorResultMetadata
    {
        /// <summary>
        /// The game domain this game install belongs to.
        /// </summary>
        public GameDomain GameDomain
        {
            get => ManuallyAddedGame.GameDomain.Get(this);
            set => ManuallyAddedGame.GameDomain.Add(this, value);
        }
        
        /// <summary>
        /// The game version for this game install.
        /// </summary>
        public string Version
        {
            get => ManuallyAddedGame.Version.Get(this);
            set => ManuallyAddedGame.Version.Add(this, value);
        }
        
        /// <summary>
        /// The path to the game install.
        /// </summary>
        public string Path
        {
            get => ManuallyAddedGame.Path.Get(this);
            set => ManuallyAddedGame.Path.Add(this, value);
        }
        
    }
}
