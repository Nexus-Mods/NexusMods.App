using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// A MneumonicDB attribute for a DateTime value
/// </summary>
/// <remarks>
/// Depending on use case, consider using transaction timestamps instead of a dedicated DateTimeAttribute.
/// </remarks>
public class DateTimeAttribute(string ns, string name) : ScalarAttribute<DateTime, long>(ValueTags.Int64, ns, name)
{
    /// <inheritdoc />
    protected override long ToLowLevel(DateTime value) => value.ToBinary();

    /// <inheritdoc />
    protected override DateTime FromLowLevel(long value, ValueTags tags) => DateTime.FromBinary(value);
}
