using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Sdk.MnemonicAttributes;

namespace NexusMods.Games.Generic.IntrinsicFiles.Models;

public partial class IniFileEntry : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.Generic.IniFileEntry";
    
    public static readonly ReferenceAttribute<IniFileDefinition> IniFile = new(Namespace, nameof(IniFile));

    public static readonly InsensitiveStringAttribute Section = new(Namespace, nameof(Section)) { IsIndexed = true };

    public static readonly InsensitiveStringAttribute Key = new(Namespace, nameof(Key)) { IsIndexed = true };
    
    public static readonly StringAttribute Value = new(Namespace, nameof(Value));
}
