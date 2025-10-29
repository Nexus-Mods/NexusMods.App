using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers.Conflicts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.DataModel;

internal partial class LoadoutManager
{
    /// <summary>
    /// Rebalances the priorities of all items in a loadout.
    /// </summary>
    private class RebalancePrioritiesTxFunc : ITxFunction
    {
        private readonly LoadoutId _loadoutId;
        private readonly EntityId[] _toSkip;

        public RebalancePrioritiesTxFunc(LoadoutId loadoutId, EntityId[] toSkip)
        {
            _loadoutId = loadoutId;

            Array.Sort(toSkip);
            _toSkip = toSkip;
        }

        public bool Equals(ITxFunction? obj)
        {
            if (obj is not RebalancePrioritiesTxFunc other) return false;
            return other._loadoutId.Equals(_loadoutId);
        }

        public override int GetHashCode() => _loadoutId.GetHashCode();

        public void Apply(ITransaction tx, IDb basis)
        {
            var priorities = LoadoutItemGroupPriority
                .FindByLoadout(basis, _loadoutId)
                .OrderBy(static model => model.Priority);

            var start = 0UL;
            foreach (var model in priorities)
            {
                var index = Array.BinarySearch(_toSkip, model.TargetId);
                if (index >= 0) continue;

                if (!model.Target.IsValid())
                {
                    tx.Delete(model.Id, recursive: false);
                    continue;
                }

                var newPriority = ConflictPriority.From(++start);
                tx.Add(model.Id, LoadoutItemGroupPriority.Priority, newPriority);
            }
        }
    }

    private class ResolveFileConflictsTxFunc : ITxFunction
    {
        private readonly LoadoutId _loadoutId;
        private readonly LoadoutItemGroupPriorityId[] _winnerIds;
        private readonly Optional<LoadoutItemGroupPriorityId> _loserId;

        public ResolveFileConflictsTxFunc(LoadoutId loadoutId, LoadoutItemGroupPriorityId[] winnerIds, Optional<LoadoutItemGroupPriorityId> loserId)
        {
            _loadoutId = loadoutId;
            _winnerIds = winnerIds;
            _loserId = loserId;
        }

        public bool Equals(ITxFunction? obj) => obj is ResolveFileConflictsTxFunc other && other._loadoutId == _loadoutId;
        public override int GetHashCode() => _loadoutId.GetHashCode();

        public void Apply(ITransaction tx, IDb basis)
        {
            var priorities = LoadoutItemGroupPriority
                .FindByLoadout(basis, _loadoutId)
                .OrderBy(static model => model.Priority)
                .ToList();

            var items = priorities
                .Where(x => _winnerIds.Contains(x.LoadoutItemGroupPriorityId))
                .ToArray();

            var loserIndex = _loserId.Convert(id => priorities.FindIndex(other => other.Id == id.Value));
 
            // remove items
            foreach (var item in items)
            {
                var index = priorities.FindIndex(other => other.Id == item.Id);
                Debug.Assert(index != -1, "should be an existing model");
                priorities.RemoveAt(index);

                if (loserIndex.HasValue && loserIndex.Value >= index) loserIndex = loserIndex.Value - 1;
            }

            // insert items
            var insertIndex = loserIndex.HasValue ? loserIndex.Value + 1 : 0;

            Debug.Assert(insertIndex >= 0 || insertIndex < priorities.Count);
            priorities.InsertRange(index: insertIndex, items);

            // recalculate new priorities
            var start = 0UL;
            foreach (var model in priorities)
            {
                var newPriority = ConflictPriority.From(++start);
                tx.Add(model.Id, LoadoutItemGroupPriority.Priority, newPriority);
            }
        }
    }

    /// <summary>
    /// Adds a priority to an item.
    /// </summary>
    private class AddPriorityTxFunc : ITxFunction
    {
        private readonly LoadoutId _loadoutId;
        private readonly LoadoutItemGroupId _targetId;

        public AddPriorityTxFunc(LoadoutId loadoutId, LoadoutItemGroupId targetId)
        {
            _loadoutId = loadoutId;
            _targetId = targetId;
        }

        public bool Equals(ITxFunction? obj)
        {
            if (obj is not AddPriorityTxFunc other) return false;
            return other._loadoutId.Equals(_loadoutId);
        }

        public override int GetHashCode() => _loadoutId.GetHashCode();

        public void Apply(ITransaction tx, IDb basis)
        {
            var priority = GetNextPriority(_loadoutId, basis);
            var id = tx.TempId();
            tx.Add(id, LoadoutItemGroupPriority.Loadout, _loadoutId);
            tx.Add(id, LoadoutItemGroupPriority.Target, _targetId);
            tx.Add(id, LoadoutItemGroupPriority.Priority, priority);
        }
    }

    private class ApplyCollectionRulesTxFunc : ITxFunction
    {
        private readonly LoadoutId _loadoutId;
        private readonly ImmutableArray<EntityId> _sortedItems;

        public ApplyCollectionRulesTxFunc(LoadoutId loadoutId, ImmutableArray<EntityId> sortedItems)
        {
            _loadoutId = loadoutId;
            _sortedItems = sortedItems;
        }

        public bool Equals(ITxFunction? other) => other is ApplyCollectionRulesTxFunc;

        public void Apply(ITransaction tx, IDb basis)
        {
            var priorities = LoadoutItemGroupPriority
                .FindByLoadout(basis, _loadoutId)
                .OrderBy(static model => model.Priority)
                .ToList();

            var prioritiesOfSortedItems = new List<LoadoutItemGroupPriority.ReadOnly>(capacity: _sortedItems.Length);
            foreach (var itemId in _sortedItems)
            {
                var index = priorities.FindIndex(priority => priority.TargetId.Value == itemId);
                if (index == -1)
                {
                    // TODO: fix items not getting a priority because they don't go through the loadout manager
                    continue;
                }

                var priority = priorities[index];
                priorities.RemoveAt(index);

                prioritiesOfSortedItems.Add(priority);
            }

            priorities.AddRange(prioritiesOfSortedItems);

            var start = 0UL;
            foreach (var model in priorities)
            {
                var newPriority = ConflictPriority.From(++start);
                tx.Add(model.Id, LoadoutItemGroupPriority.Priority, newPriority);
            }
        }
    }
}
