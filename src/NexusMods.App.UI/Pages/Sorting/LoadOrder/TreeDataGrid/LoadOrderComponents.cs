using System.ComponentModel;
using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using R3;
using static NexusMods.App.UI.Pages.Sorting.LoadOrderComponents;

namespace NexusMods.App.UI.Pages.Sorting;

public static class LoadOrderComponents
{
    public sealed class IndexComponent : ReactiveR3Object, IItemModelComponent<IndexComponent>, IComparable<IndexComponent>
    {
        private readonly ValueComponent<int> _index;
        private readonly ValueComponent<string> _displaySortIndex;

        public ReactiveCommand<Unit> MoveUp { get; }
        public ReactiveCommand<Unit> MoveDown { get; }

        public IReadOnlyBindableReactiveProperty<int> SortIndex => _index.Value;
        public IReadOnlyBindableReactiveProperty<string> DisplaySortIndex => _displaySortIndex.Value;

        public IndexComponent(ValueComponent<int> index, 
            ValueComponent<string> displaySortIndex,
            Observable<bool> canExecuteMoveUp,
            Observable<bool> canExecuteMoveDown)
        {
            _index = index;
            _displaySortIndex = displaySortIndex;
            
            MoveUp = canExecuteMoveUp.ObserveOnUIThreadDispatcher().ToReactiveCommand();
            MoveDown = canExecuteMoveDown.ObserveOnUIThreadDispatcher().ToReactiveCommand();
        }

        public int CompareTo(IndexComponent? other)
        {
            // Data is sorted by the Adapter, not the treeDataGrid, this should not be called
            throw new NotSupportedException();
        }
        
        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Disposable.Dispose(MoveUp, MoveDown);
                }
                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
        
    }
}

public static class LoadOrderColumns
{
    [UsedImplicitly]
    public sealed class IndexColumn : ICompositeColumnDefinition<IndexColumn>
    {
        public const string ColumnTemplateResourceKey = nameof(LoadOrderColumns) + "_" + nameof(IndexColumn);

        public static readonly ComponentKey IndexComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(IndexComponent));

        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            throw new NotImplementedException();
        }

        // The header name should be set on column creation as it is game dependent
        public static string GetColumnHeader() => throw new NotSupportedException();

        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;

    }

    [UsedImplicitly]
    public sealed class NameColumn : ICompositeColumnDefinition<NameColumn>
    {
        public const string ColumnTemplateResourceKey = nameof(LoadOrderColumns) + "_" + nameof(NameColumn);

        public static readonly ComponentKey NameComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + "DisplayNameComponent");
        public static readonly ComponentKey ModNameComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + "ModNameComponent");

        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            throw new NotImplementedException();
        }

        // The header name should be set on column creation as it is game dependent
        public static string GetColumnHeader() => throw new NotSupportedException();

        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    public static readonly ComponentKey IsActiveComponentKey = ComponentKey.From(nameof(LoadOrderColumns) + "_" + "IsActiveComponent");
}
