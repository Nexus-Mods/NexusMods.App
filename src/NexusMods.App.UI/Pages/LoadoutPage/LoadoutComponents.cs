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
    
    public sealed class LockedEnabledState : ReactiveR3Object, IItemModelComponent<LockedEnabledState>, IComparable<LockedEnabledState>
    {
        public int CompareTo(LockedEnabledState? other) => 0;
    }

    public sealed class EnabledStateToggle : ReactiveR3Object, IItemModelComponent<EnabledStateToggle>, IComparable<EnabledStateToggle>
    {
        public ReactiveCommand<Unit> CommandToggle { get; } = new();

        private readonly ValueComponent<bool?> _valueComponent;
        public IReadOnlyBindableReactiveProperty<bool?> Value => _valueComponent.Value;

        private readonly OneOf<ObservableHashSet<LoadoutItemId>, LoadoutItemId[]> _ids;
        public IEnumerable<LoadoutItemId> ItemIds => _ids.Match(
            f0: static x => x.AsEnumerable(),
            f1: static x => x.AsEnumerable()
        );

        public int CompareTo(EnabledStateToggle? other)
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

        public EnabledStateToggle(
            ValueComponent<bool?> valueComponent,
            LoadoutItemId itemId)
        {
            _valueComponent = valueComponent;
            _ids = new[] { itemId };

            _activationDisposable = this.WhenActivated(static (self, disposables) =>
            {
                self._valueComponent.Activate().AddTo(disposables);
            });
        }

        public EnabledStateToggle(
            ValueComponent<bool?> valueComponent,
            IObservable<IChangeSet<LoadoutItemId, EntityId>> childrenItemIdsObservable)
        {
            _valueComponent = valueComponent;
            _ids = new ObservableHashSet<LoadoutItemId>();

            _activationDisposable = this.WhenActivated((childrenItemIdsObservable), static (self, state, disposables) =>
            {
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
                    Disposable.Dispose(_activationDisposable, _valueComponent, _idsObservable ?? Disposable.Empty);
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
    public sealed class EnabledState : ICompositeColumnDefinition<EnabledState>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<LoadoutComponents.EnabledStateToggle>(key: EnabledStateToggleComponentKey);
            var bValue = a.GetOptional<LoadoutComponents.EnabledStateToggle>(key: EnabledStateToggleComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = nameof(LoadoutColumns) + "_" + nameof(EnabledState);
        public static readonly ComponentKey EnabledStateToggleComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(LoadoutComponents.EnabledStateToggle));
        public static readonly ComponentKey ParentCollectionDisabledComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(LoadoutComponents.ParentCollectionDisabled));
        public static readonly ComponentKey LockedEnabledStateComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(LoadoutComponents.LockedEnabledState));
        public static string GetColumnHeader() => "Actions";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}
