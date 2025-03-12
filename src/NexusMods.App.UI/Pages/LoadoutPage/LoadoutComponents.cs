using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using ObservableCollections;
using OneOf;
using R3;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public static class LoadoutComponents
{
    public sealed class LoadoutItemIds : ReactiveR3Object, IItemModelComponent<LoadoutItemIds>, IComparable<LoadoutItemIds>
    {
        public int CompareTo(LoadoutItemIds? other) => 0;
        
        private readonly OneOf<ObservableHashSet<LoadoutItemId>, LoadoutItemId[]> _ids;
        private readonly IDisposable? _idsObservable;
        
        public IEnumerable<LoadoutItemId> ItemIds => _ids.Match(
            f0: static x => x.AsEnumerable(),
            f1: static x => x.AsEnumerable()
        );
        
        public LoadoutItemIds(LoadoutItemId itemId)
        {
            _ids = new[] { itemId };
        }
        
        public LoadoutItemIds(IObservable<IChangeSet<LoadoutItemId, EntityId>> childrenItemIdsObservable)
        {
            _ids = new ObservableHashSet<LoadoutItemId>();
            _idsObservable = childrenItemIdsObservable.SubscribeWithErrorLogging(changeSet => _ids.AsT0.ApplyChanges(changeSet));
        }
        
        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (disposing)
                {
                    Disposable.Dispose(_idsObservable ?? Disposable.Empty);
                }
            }

            base.Dispose(disposing);
        }
    }
    
    public sealed class ParentCollectionDisabled : ReactiveR3Object, IItemModelComponent<ParentCollectionDisabled>, IComparable<ParentCollectionDisabled>
    {
        public ReactiveCommand<NavigationInformation, NavigationInformation> ButtonCommand { get; } = new(info => info);

        public int CompareTo(ParentCollectionDisabled? other) => 0;
    }
    
    public sealed class LockedEnabledState : ReactiveR3Object, IItemModelComponent<LockedEnabledState>, IComparable<LockedEnabledState>
    {
        public ReactiveCommand<NavigationInformation, NavigationInformation> ButtonCommand { get; } = new(info => info);

        public int CompareTo(LockedEnabledState? other) => 0;
    }

    public sealed class MixLockedAndParentDisabled : ReactiveR3Object, IItemModelComponent<MixLockedAndParentDisabled>, IComparable<MixLockedAndParentDisabled>
    {
        public ReactiveCommand<NavigationInformation, NavigationInformation> ButtonCommand { get; } = new(info => info);
        
        public int CompareTo(MixLockedAndParentDisabled? other) => 0;
    }

    public sealed class EnabledStateToggle : ReactiveR3Object, IItemModelComponent<EnabledStateToggle>, IComparable<EnabledStateToggle>
    {
        public ReactiveCommand<Unit> CommandToggle { get; } = new();

        private readonly ValueComponent<bool?> _valueComponent;
        public IReadOnlyBindableReactiveProperty<bool?> Value => _valueComponent.Value;

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

        public EnabledStateToggle(ValueComponent<bool?> valueComponent)
        {
            _valueComponent = valueComponent;

            _activationDisposable = this.WhenActivated(static (self, disposables) =>
            {
                self._valueComponent.Activate().AddTo(disposables);
            });
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (disposing)
                {
                    Disposable.Dispose(_activationDisposable, _valueComponent);
                }                
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
            var bValue = b.GetOptional<StringComponent>(key: ComponentKey);
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
            var bValue = b.GetOptional<LoadoutComponents.EnabledStateToggle>(key: EnabledStateToggleComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = nameof(LoadoutColumns) + "_" + nameof(EnabledState);
        public static readonly ComponentKey LoadoutItemIdsComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(LoadoutComponents.LoadoutItemIds));
        public static readonly ComponentKey EnabledStateToggleComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(LoadoutComponents.EnabledStateToggle));
        public static readonly ComponentKey ParentCollectionDisabledComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(LoadoutComponents.ParentCollectionDisabled));
        public static readonly ComponentKey LockedEnabledStateComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(LoadoutComponents.LockedEnabledState));
        public static readonly ComponentKey MixLockedAndParentDisabledComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(LoadoutComponents.MixLockedAndParentDisabled));
        public static string GetColumnHeader() => "Actions";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}
