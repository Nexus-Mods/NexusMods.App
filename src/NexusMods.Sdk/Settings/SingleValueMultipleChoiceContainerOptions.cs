using JetBrains.Annotations;

namespace NexusMods.Sdk.Settings;

[PublicAPI]
public class SingleValueMultipleChoiceContainerOptions : IContainerOptions
{
    public IEqualityComparer<object> ValueComparer { get; }
    public object[] AllowedValues { get; }
    public Func<object, string> ValueToDisplayString { get; }

    private SingleValueMultipleChoiceContainerOptions(
        IEqualityComparer<object> valueComparer,
        object[] allowedValues,
        Func<object, string> valueToDisplayString)
    {
        ValueComparer = valueComparer;
        AllowedValues = allowedValues;
        ValueToDisplayString = valueToDisplayString;
    }

    public static SingleValueMultipleChoiceContainerOptions Create<TProperty>(
        IEqualityComparer<TProperty> valueComparer,
        TProperty[] allowedValues,
        Func<TProperty, string> valueToDisplayString
    ) where TProperty : notnull
    {
        return new SingleValueMultipleChoiceContainerOptions(
            valueComparer: EqualityComparer<object>.Create((a, b) =>
            {
                if (a is null && b is null) return true;
                if (a is null) return false;
                if (b is null) return false;

                var valueA = Cast<TProperty>(a);
                var valueB = Cast<TProperty>(b);
                return valueComparer.Equals(valueA, valueB);
            }),
            allowedValues: allowedValues.Select(x => (object)x).ToArray(),
            valueToDisplayString: obj => valueToDisplayString(Cast<TProperty>(obj))
        );
    }

    private static TProperty Cast<TProperty>(object obj)
    {
        if (obj is not TProperty value) throw new ArgumentException($"Expected value to be `{typeof(TProperty)}` but received `{obj.GetType()}`");
        return value;
    }
}
