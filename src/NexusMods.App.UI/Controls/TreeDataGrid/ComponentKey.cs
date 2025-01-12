using JetBrains.Annotations;
using TransparentValueObjects;

namespace NexusMods.App.UI.Controls;

[PublicAPI]
[ValueObject<string>]
public readonly partial struct ComponentKey
{
    public static implicit operator ComponentKey(Type type) => From(type.ToString());
}
