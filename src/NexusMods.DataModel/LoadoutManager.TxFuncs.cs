using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers.Conflicts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.DataModel;

internal partial class LoadoutManager
{
    private class RebalancePrioritiesTxFunc : ITxFunction
    {
        private readonly LoadoutId _loadoutId;
        private readonly EntityId[] _toSkip;

        public RebalancePrioritiesTxFunc(LoadoutId loadoutId, EntityId[] toSkip)
        {
            _loadoutId = loadoutId;
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
                if (_toSkip.Contains(model.TargetId)) continue;
                var newPriority = ConflictPriority.From(++start);
                tx.Add(model.Id, LoadoutItemGroupPriority.Priority, newPriority);
            }
        }
    }

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
            return other._loadoutId.Equals(_loadoutId) && other._targetId == _targetId;
        }

        public override int GetHashCode() => HashCode.Combine(_loadoutId, _targetId);

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
