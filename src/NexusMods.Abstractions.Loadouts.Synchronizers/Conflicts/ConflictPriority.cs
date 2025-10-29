using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using TransparentValueObjects;

namespace NexusMods.Abstractions.Loadouts.Synchronizers.Conflicts;

/// <summary>
/// Conflict priority is defined as higher number = winning and lower number = losing.
/// </summary>
[PublicAPI]
[ValueObject<ulong>]
public readonly partial struct ConflictPriority
{
    public static readonly ConflictPriority MaxValue = From(ulong.MaxValue);
    public static readonly ConflictPriority MinValue = From(ulong.MinValue);
}

public class ConflictPriorityAttribute : ScalarAttribute<ConflictPriority, ulong, UInt64Serializer>
{
    public ConflictPriorityAttribute(string ns, string name) : base(ns, name) { }

    public override ulong ToLowLevel(ConflictPriority value) => value.Value;

    public override ConflictPriority FromLowLevel(ulong value, AttributeResolver resolver) => ConflictPriority.From(value);
}
