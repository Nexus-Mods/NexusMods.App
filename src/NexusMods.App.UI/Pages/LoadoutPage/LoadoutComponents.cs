using DynamicData;
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
    public sealed class IsEnabled : ReactiveR3Object, IItemModelComponent<IsEnabled>, IComparable<IsEnabled>
    {
        public ReactiveCommand<Unit> CommandToggle { get; } = new();

        private readonly ValueComponent<bool?> _valueComponent;
        public BindableReactiveProperty<bool?> Value => _valueComponent.Value;

        private readonly OneOf<ObservableHashSet<LoadoutItemId>, LoadoutItemId[]> _ids;

        public IEnumerable<LoadoutItemId> ItemIds => _ids.Match(
            f0: static x => x.AsEnumerable(),
            f1: static x => x.AsEnumerable()
        );

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

        public IsEnabled(
            ValueComponent<bool?> valueComponent,
            LoadoutItemId itemId)
        {
            _valueComponent = valueComponent;
            _ids = new[] { itemId };

            _activationDisposable = this.WhenActivated((self, disposables) =>
            {
                self._valueComponent.Activate().AddTo(disposables);
            });
        }

        public IsEnabled(
            ValueComponent<bool?> valueComponent,
            IObservable<IChangeSet<LoadoutItemId, EntityId>> childrenItemIdsObservable)
        {
            _valueComponent = valueComponent;
            _ids = new ObservableHashSet<LoadoutItemId>();

            _activationDisposable = this.WhenActivated(childrenItemIdsObservable, static (self, state, disposables) =>
            {
                var childrenItemIdsObservable = state;
                self._valueComponent.Activate().AddTo(disposables);

                childrenItemIdsObservable
                    .SubscribeWithErrorLogging(changeSet => self._ids.AsT0.ApplyChanges(changeSet))
                    .AddTo(disposables);

                Disposable.Create(self._ids.AsT0, static set => set.Clear()).AddTo(disposables);
            });
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                Disposable.Dispose(_activationDisposable, _valueComponent);
                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
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
