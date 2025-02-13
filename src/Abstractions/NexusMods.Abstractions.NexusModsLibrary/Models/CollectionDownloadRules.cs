using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary.Models;

/// <summary>
/// Represents a parsed collection Vortex mod rule.
/// </summary>
public partial class CollectionDownloadRules : IModelDefinition
{
    private const string Namespace = "NexusMods.NexusModsLibrary.CollectionDownloadRules";

    /// <summary>
    /// Reference to the source download.
    /// </summary>
    public static readonly ReferenceAttribute<CollectionDownload> Source = new(Namespace, nameof(Source));

    /// <summary>
    /// Reference to the other download.
    /// </summary>
    public static readonly ReferenceAttribute<CollectionDownload> Other = new(Namespace, nameof(Other));

    /// <summary>
    /// Rule type.
    /// </summary>
    public static readonly EnumAttribute<CollectionDownloadRuleType> RuleType = new(Namespace, nameof(RuleType));

    /// <summary>
    /// Index into the source array.
    /// </summary>
    public static readonly Int32Attribute ArrayIndex = new(Namespace, nameof(ArrayIndex)) { IsIndexed = true };
}

/// <summary>
/// Rule types.
/// </summary>
public enum CollectionDownloadRuleType
{
    /// <summary>
    /// <see cref="CollectionDownloadRules.Source"/> comes before <see cref="CollectionDownloadRules.Other"/>.
    /// </summary>
    Before = 0,

    /// <summary>
    /// <see cref="CollectionDownloadRules.Source"/> comes after <see cref="CollectionDownloadRules.Other"/>.
    /// </summary>
    After = 1,
}
