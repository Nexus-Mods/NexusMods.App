using System.Collections.Frozen;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using R3;

namespace NexusMods.App.UI.Pages.Sorting;

public static class FileConflictsComponents
{
    public class NeighbourIds : IItemModelComponent<NeighbourIds>, IComparable<NeighbourIds>
    {
        public EntityId Prev { get; set; }
        public EntityId Next { get; set; }

        public NeighbourIds(EntityId prev, EntityId next)
        {
            Prev = prev;
            Next = next;
        }

        public int CompareTo(NeighbourIds? other) => 0;
    }

    public class NumConflicts : IItemModelComponent<NumConflicts>, IComparable<NumConflicts>
    {
        public ValueComponent<int> NumWinners { get; }
        public ValueComponent<int> NumLosers { get; }

        public IReadOnlyBindableReactiveProperty<bool> HasWinners { get; }
        public IReadOnlyBindableReactiveProperty<bool> HasLosers { get; }

        public NumConflicts(ValueComponent<int> numWinners, ValueComponent<int> numLosers)
        {
            NumWinners = numWinners;
            NumLosers = numLosers;

            HasWinners = numWinners.Value.Select(x => x > 0).ToReadOnlyBindableReactiveProperty();
            HasLosers = numLosers.Value.Select(x => x > 0).ToReadOnlyBindableReactiveProperty();
        }

        public int CompareTo(NumConflicts? other)
        {
            if (other is null) return 1;

            var winnerComparison = NumWinners.CompareTo(other.NumWinners);
            if (winnerComparison != 0) return winnerComparison;

            return NumLosers.CompareTo(other.NumLosers);
        }
    }
    
    public class ViewAction : IItemModelComponent<ViewAction>, IComparable<ViewAction>
    {
        public LoadoutItemGroup.ReadOnly Group { get; }
        private readonly LoadoutFile.ReadOnly[] _loadoutFiles;
        private readonly FrozenDictionary<GamePath, FileConflictGroup> _conflictsByPath;
        public ReactiveCommand<Unit> CommandViewConflicts { get; } = new ReactiveCommand();

        public ViewAction(LoadoutItemGroup.ReadOnly group, LoadoutFile.ReadOnly[] loadoutFiles, FrozenDictionary<GamePath, FileConflictGroup> conflictsByPath)
        {
            Group = group;
            _loadoutFiles = loadoutFiles;
            _conflictsByPath = conflictsByPath;
        }

        public int CompareTo(ViewAction? other) => 0;

        public string CreateMarkdown()
        {
            var markdown = _loadoutFiles
                .Select(GamePath (x) => x.AsLoadoutItemWithTargetPath().TargetPath)
                .Select(gamePath =>
                {
                    var conflicts = _conflictsByPath[gamePath].Items;
                    var conflictingGroups = conflicts.Where(x => x.File.IsT0).Select(x => x.File.AsT0.AsLoadoutItemWithTargetPath().AsLoadoutItem().Parent).ToArray();
                    return (gamePath, conflictingGroups);
                })
                .Select(tuple =>
                {
                    var (gamePath, conflictingGroups) = tuple;
                    var heading = $"## {gamePath}\n";
                    var body = conflictingGroups.Select(x => $"- {x.AsLoadoutItem().Name}").Aggregate((a, b) => $"{a}\n{b}");

                    return $"{heading}\n{body}";
                })
                .Aggregate((a,b) => $"{a}\n{b}");

            return markdown;
        }
    }
}

public static class FileConflictsColumns
{
    private const string Prefix = $"{nameof(FileConflictsColumns)}_";

    public sealed class ConflictsColumn : ICompositeColumnDefinition<ConflictsColumn>
    {
        public const string ColumnTemplateResourceKey = Prefix + nameof(ConflictsColumn);
        public static readonly ComponentKey NumConflictsComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(FileConflictsComponents.NumConflicts));

        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            return a.GetOptional<FileConflictsComponents.NumConflicts>(NumConflictsComponentKey).Compare(b.GetOptional<FileConflictsComponents.NumConflicts>(NumConflictsComponentKey));
        }

        public static string GetColumnHeader() => "Conflicts";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
    
    public sealed class IndexColumn : ICompositeColumnDefinition<IndexColumn>
    {
        public const string ColumnTemplateResourceKey = Prefix + nameof(IndexColumn);
        public static readonly ComponentKey IndexComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(SharedComponents.IndexComponent));
        public static readonly ComponentKey NeighbourIdsComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(FileConflictsComponents.NeighbourIds));

        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            // tree is force sorted externally
            throw new NotImplementedException();
        }

        public static string GetColumnHeader() => "Conflict Priority";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    public class Actions : ICompositeColumnDefinition<Actions>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            return a.GetOptional<FileConflictsComponents.ViewAction>(ViewComponentKey).Compare(b.GetOptional<FileConflictsComponents.ViewAction>(ViewComponentKey));
        }

        public const string ColumnTemplateResourceKey = Prefix + "Action";
        public static readonly ComponentKey ViewComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(Actions) + "_" + "View");

        public static string GetColumnHeader() => "Actions";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}
