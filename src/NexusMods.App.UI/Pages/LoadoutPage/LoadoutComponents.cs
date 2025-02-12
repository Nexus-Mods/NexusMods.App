using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using ObservableCollections;
using OneOf;
using R3;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public static class LoadoutComponents
{
    public sealed class ParentCollectionDisabled : ReactiveR3Object, IItemModelComponent<ParentCollectionDisabled>, IComparable<ParentCollectionDisabled>
    {
        public int CompareTo(ParentCollectionDisabled? other) => 0;
    }

    public sealed class IsEnabled : ReactiveR3Object, IItemModelComponent<IsEnabled>, IComparable<IsEnabled>
    {
        public ReactiveCommand<Unit> CommandToggle { get; } = new();

        private readonly ValueComponent<bool?> _valueComponent;
        public IReadOnlyBindableReactiveProperty<bool?> Value => _valueComponent.Value;

        private readonly OneOf<ObservableHashSet<LoadoutItemId>, LoadoutItemId[]> _ids;
        public IEnumerable<LoadoutItemId> ItemIds => _ids.Match(
            f0: static x => x.AsEnumerable(),
            f1: static x => x.AsEnumerable()
        );

        private readonly ValueComponent<bool> _isLockedComponent;
        public IReadOnlyBindableReactiveProperty<bool> IsLocked => _isLockedComponent.Value;

        public int CompareTo(IsEnabled? other)
        {
            var (a, b) = (Value.Value, other?.Value.Value);
            return (a, b) switch
            {
                (null, null) => 0,
                (not null, null) => 1,
                (null, not null) => -1,
                (not null, not null) => a.Value.CompareTo(b.Value),
            };
        }

        private readonly IDisposable _activationDisposable;
        private readonly IDisposable? _idsObservable;

        public IsEnabled(
            ValueComponent<bool?> valueComponent,
            LoadoutItemId itemId,
            bool isLocked)
        {
            _valueComponent = valueComponent;
            _ids = new[] { itemId };
            _isLockedComponent = new ValueComponent<bool>(value: isLocked);

            _activationDisposable = this.WhenActivated(static (self, disposables) =>
            {
                self._valueComponent.Activate().AddTo(disposables);
            });
        }

        public IsEnabled(
            ValueComponent<bool?> valueComponent,
            IObservable<IChangeSet<LoadoutItemId, EntityId>> childrenItemIdsObservable,
            ValueComponent<bool> isLockedComponent)
        {
            _valueComponent = valueComponent;
            _ids = new ObservableHashSet<LoadoutItemId>();
            _isLockedComponent = isLockedComponent;

            _activationDisposable = this.WhenActivated((childrenItemIdsObservable), static (self, state, disposables) =>
            {
                self._isLockedComponent.Activate().AddTo(disposables);
                self._valueComponent.Activate().AddTo(disposables);
            });

            _idsObservable = childrenItemIdsObservable.SubscribeWithErrorLogging(changeSet => _ids.AsT0.ApplyChanges(changeSet));
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Disposable.Dispose(_activationDisposable, _valueComponent, _isLockedComponent, _idsObservable ?? Disposable.Empty);
                }

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }
}

public static class LoadoutColumns
{
    [UsedImplicitly]
    public sealed class Collections : ICompositeColumnDefinition<Collections>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<StringComponent>(key: ComponentKey);
            var bValue = a.GetOptional<StringComponent>(key: ComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = nameof(LoadoutColumns) + "_" + nameof(Collections);
        public static readonly ComponentKey ComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(StringComponent));
        public static string GetColumnHeader() => "Collections";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    [UsedImplicitly]
    public sealed class IsEnabled : ICompositeColumnDefinition<IsEnabled>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<LoadoutComponents.IsEnabled>(key: IsEnabledComponentKey);
            var bValue = a.GetOptional<LoadoutComponents.IsEnabled>(key: IsEnabledComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = nameof(LoadoutColumns) + "_" + nameof(IsEnabled);
        public static readonly ComponentKey IsEnabledComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(LoadoutComponents.IsEnabled));
        public static readonly ComponentKey ParentCollectionDisabledComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(LoadoutComponents.ParentCollectionDisabled));
        public static string GetColumnHeader() => "Actions";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}
