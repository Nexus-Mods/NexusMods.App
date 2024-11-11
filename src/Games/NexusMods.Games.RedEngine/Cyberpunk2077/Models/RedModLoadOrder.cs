using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Models;

/// <summary>
/// Represents the RedMod load order
/// </summary>
[PublicAPI]
[Include<Abstractions.Loadouts.LoadOrder>]
public partial class RedModLoadOrder : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.RedEngine.Cyberpunk2077.RedModLoadOrder";
    
    /// <summary>
    /// This value should be updated every time the load order is changed, used for detecting changes.
    /// </summary>
    public static readonly UInt32Attribute Revision = new(Namespace, nameof(Revision)); }
