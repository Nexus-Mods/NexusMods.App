using System.Diagnostics;
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

                var newPriority = ConflictPriority.From(++start);
                tx.Add(model.Id, LoadoutItemGroupPriority.Priority, newPriority);
            }
        }
    }

    /// <summary>
    /// Moves targets before or after the anchor.
    /// </summary>
    private class MoveFileConflicts : ITxFunction
    {
        private readonly LoadoutId _loadoutId;
        private readonly LoadoutItemGroupPriorityId _anchorId;
        private readonly LoadoutItemGroupPriorityId[] _targetIds;
        private readonly bool _moveTargetsBeforeAnchor;

        public MoveFileConflicts(LoadoutId loadoutId, LoadoutItemGroupPriorityId anchorId, LoadoutItemGroupPriorityId[] targetIds, bool moveTargetsBeforeAnchor)
        {
            _loadoutId = loadoutId;
            _anchorId = anchorId;
            _targetIds = targetIds;
            _moveTargetsBeforeAnchor = moveTargetsBeforeAnchor;
        }

        public bool Equals(ITxFunction? obj) => obj is MoveFileConflicts other && other._loadoutId == _loadoutId;
        public override int GetHashCode() => _loadoutId.GetHashCode();

        public void Apply(ITransaction tx, IDb basis)
        {
            var priorities = LoadoutItemGroupPriority
                .FindByLoadout(basis, _loadoutId)
                .OrderBy(static model => model.Priority)
                .ToList();

            var targets = _targetIds
                .Select(id => LoadoutItemGroupPriority.Load(basis, id))
                .OrderBy(x => x.Priority)
                .ToArray();

            // remove targets
            foreach (var target in targets)
            {
                if (target.LoadoutId != _loadoutId) throw new ArgumentException($"Expected target {target.Id} to be in the same loadout {_loadoutId} as the anchor {_anchorId} but found {target.LoadoutId}");

                var index = priorities.FindIndex(other => other.Id == target.Id);
                Debug.Assert(index != -1, "should be an existing model");

                priorities.RemoveAt(index);
            }

            // insert targets
            var anchorIndex = priorities.FindIndex(other => other.Id == _anchorId.Value);
            Debug.Assert(anchorIndex != -1, "anchor should be an existing model");

            var relative = _moveTargetsBeforeAnchor ? 0 : +1;
            priorities.InsertRange(index: anchorIndex + relative, targets);

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
}
