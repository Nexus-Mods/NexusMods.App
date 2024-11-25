using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.App.UI.Pages.Sorting
{
    public class MockLoadoutSortableItemProvider : ILoadoutSortableItemProvider
    {
        private ReadOnlyObservableCollection<ISortableItem> _sortableItems;
        public ISortableItemProviderFactory ParentFactory { get; } = null!;
        public LoadoutId LoadoutId { get; }

        ReadOnlyObservableCollection<ISortableItem> ILoadoutSortableItemProvider.SortableItems => _sortableItems;

        public Task SetRelativePosition(ISortableItem sortableItem, int delta)
        {
            throw new NotImplementedException();
        }

        public MockLoadoutSortableItemProvider()
        {
            var items = new List<ISortableItem>
            {
                new MockSortableItem { ItemId = Guid.NewGuid(), DisplayName = "Item 1", ModName = "Mod 1", SortableItemProvider = this, IsActive = true},
                new MockSortableItem { ItemId = Guid.NewGuid(), DisplayName = "Item 2", ModName = "Mod 2", SortableItemProvider = this, IsActive = true},
            };

            _sortableItems = new ReadOnlyObservableCollection<ISortableItem>(new ObservableCollection<ISortableItem>(items));
        }
    }

    public class MockSortableItem : ISortableItem
    {
        public bool IsActive { get; set; }
        public Guid ItemId { get; set; }
        public ILoadoutSortableItemProvider SortableItemProvider { get; set; } = null!;
        public int SortIndex { get; set; }
        public string DisplayName { get; set; } = "Display Name";
        public string ModName { get; set; } = "Mod Name";
    }
}
