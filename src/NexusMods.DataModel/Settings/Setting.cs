using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.DataModel.Settings;

/// <summary>
/// Settings are stored as a simple key-value pair.
/// </summary>
public static class Setting
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
    
    /// <summary>
    /// Model for a specific setting
    /// </summary>
    /// <param name="tx"></param>
    public class Model(ITransaction tx) : Entity(tx)
    {
        /// <summary>
        /// Name of the setting
        /// </summary>
        public string Name
        {
            get => Setting.Name.Get(this);
            set => Setting.Name.Add(this, value);
        }
        
        /// <summary>
        /// Value of the setting
        /// </summary>
        public string Value
        {
            get => Setting.Value.Get(this);
            set => Setting.Value.Add(this, value);
        }
    }
}
