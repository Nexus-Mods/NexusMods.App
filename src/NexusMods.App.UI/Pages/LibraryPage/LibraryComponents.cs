using System.Diagnostics.CodeAnalysis;
using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using ObservableCollections;
using OneOf;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public static class LibraryColumns
{
    [UsedImplicitly]
    public sealed class ItemVersion : ICompositeColumnDefinition<ItemVersion>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<StringComponent>(key: CurrentVersionComponentKey);
            var bValue = b.GetOptional<StringComponent>(key: CurrentVersionComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = nameof(LibraryColumns) + "_" + nameof(ItemVersion);
        public static readonly ComponentKey CurrentVersionComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(ItemVersion) + "_" + "Current");
        public static readonly ComponentKey NewVersionComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(ItemVersion) + "_" + "New");

        public static string GetColumnHeader() => "Version";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    [UsedImplicitly]
    public sealed class DownloadedDate : ICompositeColumnDefinition<DownloadedDate>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<DateComponent>(ComponentKey);
            var bValue = b.GetOptional<DateComponent>(ComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = nameof(LibraryColumns) + "_" + nameof(DownloadedDate);
        public static readonly ComponentKey ComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(DateComponent));

        public static string GetColumnHeader() => "Downloaded";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    [UsedImplicitly]
    public sealed class ItemSize : ICompositeColumnDefinition<ItemSize>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<SizeComponent>(key: ComponentKey);
            var bValue = b.GetOptional<SizeComponent>(key: ComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = nameof(LibraryColumns) + "_" + nameof(ItemSize);
        public static readonly ComponentKey ComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(SizeComponent));
        public static string GetColumnHeader() => "Size";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    [UsedImplicitly]
    public sealed class Actions : ICompositeColumnDefinition<Actions>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<LibraryComponents.InstallAction>(key: InstallComponentKey);
            var bValue = b.GetOptional<LibraryComponents.InstallAction>(key: InstallComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = nameof(LibraryColumns) + "_" + nameof(Actions);

        public static readonly ComponentKey InstallComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(Actions) + "_" + "Install");
        public static readonly ComponentKey DownloadComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(Actions) + "_" + "Download");
        public static string GetColumnHeader() => "Action";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}

public static class LibraryComponents
{
    public sealed class InstallAction : ReactiveR3Object, IItemModelComponent<InstallAction>, IComparable<InstallAction>
    {
        public IReadOnlyBindableReactiveProperty<bool> IsInstalled { get; }
        public IReadOnlyBindableReactiveProperty<string> ButtonText { get; }
        public ReactiveCommand<Unit> CommandInstall { get; }

        private readonly OneOf<ObservableHashSet<LibraryItemId>, LibraryItemId[]> _ids;
        public IEnumerable<LibraryItemId> ItemIds => _ids.Match(
            f0: static x => x.AsEnumerable(),
            f1: static x => x.AsEnumerable()
        );

        public int CompareTo(InstallAction? other)
        {
            if (other is null) return 1;
            return IsInstalled.Value.CompareTo(other.IsInstalled.Value);
        }

        private readonly ReactiveR3Object _source;
        private readonly IDisposable _activationDisposable;

        public InstallAction(
            ValueComponent<bool> isInstalled,
            LibraryItemId itemId)
        {
            _source = isInstalled;
            _ids = new[] { itemId };

            IsInstalled = isInstalled.Value;

            CommandInstall = isInstalled.Value
                .Select(static isInstalled => !isInstalled)
                .ToReactiveCommand<Unit>();

            ButtonText = isInstalled.Value
                .Select(GetButtonText)
                .ToReadOnlyBindableReactiveProperty(initialValue: GetButtonText(isInstalled.Value.Value));

            _activationDisposable = this.WhenActivated(static (self, disposables) =>
            {
                self._source.Activate().AddTo(disposables);
            });
        }

        public InstallAction(
            ValueComponent<MatchesData> matches,
            IObservable<IChangeSet<LibraryItemId, EntityId>> childrenItemIdsObservable)
        {
            _source = matches;
            _ids = new ObservableHashSet<LibraryItemId>();

            IsInstalled = matches.Value
                .Select(static data => data.NumMatches > 0)
                .ToReadOnlyBindableReactiveProperty();

            CommandInstall = IsInstalled
                .AsObservable()
                .Select(static isInstalled => !isInstalled)
                .ToReactiveCommand<Unit>();

            ButtonText = matches.Value
                .Select(static tuple => GetButtonText(tuple, isExpanded: false))
                .ToReadOnlyBindableReactiveProperty(initialValue: GetButtonText(matches.Value.Value, isExpanded: false));

            _activationDisposable = this.WhenActivated(childrenItemIdsObservable, static (self, state, disposables) =>
            {
                self._source.Activate().AddTo(disposables);

                var childrenItemIdsObservable = state;
                childrenItemIdsObservable
                    .SubscribeWithErrorLogging(changeSet => self._ids.AsT0.ApplyChanges(changeSet))
                    .AddTo(disposables);

                Disposable.Create(self._ids.AsT0, static set => set.Clear()).AddTo(disposables);
            });
        }

        private static string GetButtonText(bool isInstalled) => isInstalled ? "Installed" : "Install";

        [SuppressMessage("ReSharper", "RedundantIfElseBlock")]
        [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
        private static string GetButtonText(MatchesData matchesData, bool isExpanded)
        {
            var (numInstalled, numTotal) = matchesData;

            if (numInstalled > 0)
            {
                if (numInstalled == numTotal)
                {
                    return "Installed";
                } else {
                    return $"Installed {numInstalled}/{numTotal}";
                }
            }
            else
            {
                if (!isExpanded && numTotal == 1)
                {
                    return "Install";
                } else {
                    return $"Install ({numTotal})";
                }
            }
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Disposable.Dispose(_activationDisposable, CommandInstall, ButtonText, _source);
                }

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
