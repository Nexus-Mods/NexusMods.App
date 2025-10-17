using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Backend;

/// <summary>
/// Settings are stored as a simple key-value pair.
/// </summary>
public partial class Setting : IModelDefinition
{
    private const string Namespace = "NexusMods.DataModel.Settings";

    /// <summary>
    /// Name of the setting
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name)) {IsIndexed = true};

    /// <summary>
    /// Value of the setting
    /// </summary>
    public static readonly StringAttribute Value = new(Namespace, nameof(Value));
}
