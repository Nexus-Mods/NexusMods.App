using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using R3;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public static class LoadoutComponents
{
    public sealed class IsEnabled : AValueComponent<bool>, IItemModelComponent<IsEnabled>, IComparable<IsEnabled>
    {
        public ReactiveCommand<Unit> CommandToggle { get; } = new();

        public int CompareTo(IsEnabled? other) => Value.Value.CompareTo(other?.Value.Value ?? false);

        public IsEnabled(
            bool initialValue,
            IObservable<bool> valueObservable,
            bool subscribeWhenCreated = false) : base(initialValue, valueObservable, subscribeWhenCreated) { }

        public IsEnabled(
            bool initialValue,
            Observable<bool> valueObservable,
            bool subscribeWhenCreated = false) : base(initialValue, valueObservable, subscribeWhenCreated) { }

        public IsEnabled(bool value) : base(value) { }
    }
}

public static class LoadoutColumns
{
    public sealed class IsEnabled : ICompositeColumnDefinition<IsEnabled>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<LoadoutComponents.IsEnabled>(key: ComponentKey);
            var bValue = a.GetOptional<LoadoutComponents.IsEnabled>(key: ComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = nameof(LoadoutColumns) + "_" + nameof(IsEnabled);
        public static readonly ComponentKey ComponentKey = typeof(LoadoutComponents.IsEnabled);
        public static string GetColumnHeader() => "Enabled";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}
