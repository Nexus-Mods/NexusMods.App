using System.Collections.Frozen;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using R3;

namespace NexusMods.App.UI.Pages.Sorting;

public static class FileConflictsComponents
{
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
    
    public sealed class IndexColumn : ICompositeColumnDefinition<IndexColumn>
    {
        public const string ColumnTemplateResourceKey = Prefix + nameof(IndexColumn);
        public static readonly ComponentKey IndexComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(SharedComponents.IndexComponent));

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
