using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Filters;
using NexusMods.App.UI.Controls.TreeDataGrid.Filters;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Resources;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Icons;
using ObservableCollections;
using OneOf;
using R3;
using Disposable = R3.Disposable;

namespace NexusMods.App.UI.Pages.LibraryPage;

public static class LibraryColumns
{
    
    [UsedImplicitly]
    public sealed class ItemVersion : ICompositeColumnDefinition<ItemVersion>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aVersion = a.GetOptional<VersionComponent>(key: CurrentVersionComponentKey);
            var bVersion = b.GetOptional<VersionComponent>(key: CurrentVersionComponentKey);
            
            var aNewVersion = a.GetOptional<LibraryComponents.NewVersionAvailable>(key: NewVersionComponentKey);
            var bNewVersion = b.GetOptional<LibraryComponents.NewVersionAvailable>(key: NewVersionComponentKey);
            
            // Compare based on the presence of new versions
            return (aNewVersion.HasValue, bNewVersion.HasValue) switch
            {
                (true, false) => 1,
                (false, true) => -1,
                _ => aVersion.Compare(bVersion),
            };
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
    public sealed class Collections : ICompositeColumnDefinition<Collections>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<LibraryComponents.RelatedCollectionsComponent>(key: RelatedCollectionsComponentKey);
            var bValue = b.GetOptional<LibraryComponents.RelatedCollectionsComponent>(key: RelatedCollectionsComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = nameof(LibraryColumns) + "_" + nameof(Collections);
        public static readonly ComponentKey RelatedCollectionsComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(LibraryComponents.RelatedCollectionsComponent));
        public static string GetColumnHeader() => "Collections";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    [UsedImplicitly]
    public sealed class Actions : ICompositeColumnDefinition<Actions>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aInstall = a.GetOptional<LibraryComponents.InstallAction>(key: InstallComponentKey);
            var bInstall = b.GetOptional<LibraryComponents.InstallAction>(key: InstallComponentKey);

            var aUpdate = a.GetOptional<LibraryComponents.UpdateAction>(key: UpdateComponentKey);
            var bUpdate = b.GetOptional<LibraryComponents.UpdateAction>(key: UpdateComponentKey);

            var aViewChangelog = a.GetOptional<LibraryComponents.ViewChangelogAction>(key: ViewChangelogComponentKey);
            var bViewChangelog = b.GetOptional<LibraryComponents.ViewChangelogAction>(key: ViewChangelogComponentKey);

            var aViewModPage = a.GetOptional<SharedComponents.ViewModPageAction>(key: ViewModPageComponentKey);
            var bViewModPage = b.GetOptional<SharedComponents.ViewModPageAction>(key: ViewModPageComponentKey);

            var aHideUpdates = a.GetOptional<LibraryComponents.HideUpdatesAction>(key: HideUpdatesComponentKey);
            var bHideUpdates = b.GetOptional<LibraryComponents.HideUpdatesAction>(key: HideUpdatesComponentKey);
            
            var aDeleteItem = a.GetOptional<LibraryComponents.DeleteItemAction>(key: DeleteItemComponentKey);
            var bDeleteItem = b.GetOptional<LibraryComponents.DeleteItemAction>(key: DeleteItemComponentKey);

            var orderA = GetOrder(aInstall, aUpdate, aViewChangelog, aViewModPage, aHideUpdates, aDeleteItem);
            var orderB = GetOrder(bInstall, bUpdate, bViewChangelog, bViewModPage, bHideUpdates, bDeleteItem);

            return orderA.CompareTo(orderB);
            
            int GetOrder(
                Optional<LibraryComponents.InstallAction> install, 
                Optional<LibraryComponents.UpdateAction> update, 
                Optional<LibraryComponents.ViewChangelogAction> viewChangelog, 
                Optional<SharedComponents.ViewModPageAction> viewModPage, 
                Optional<LibraryComponents.HideUpdatesAction> hideUpdates, 
                Optional<LibraryComponents.DeleteItemAction> deleteItem)
            {
                var isUpdateAvailable = update.HasValue;
                var hasInstallButton = install is { HasValue: true, Value.IsInstalled.Value: false };
                var isInstalled = install is { HasValue: true, Value.IsInstalled.Value: true };
                var hasViewChangelog = viewChangelog.HasValue;
                var hasViewModPage = viewModPage.HasValue;
                var hasHideUpdates = hideUpdates.HasValue;
                var hasDeleteItem = deleteItem.HasValue;

                if (isUpdateAvailable && hasInstallButton) return 1;
                if (hasInstallButton) return 2;
                if (isUpdateAvailable) return 3;
                if (isInstalled) return 4;
                if (hasViewChangelog || hasViewModPage || hasHideUpdates) return 5;
                if (hasDeleteItem) return 6;
                return 7;
            }
        }

        public const string ColumnTemplateResourceKey = nameof(LibraryColumns) + "_" + nameof(Actions);
        
        public static readonly ComponentKey LibraryItemIdsComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(LibraryComponents.LibraryItemIds));
        public static readonly ComponentKey InstallComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(Actions) + "_" + "Install");
        public static readonly ComponentKey UpdateComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(Actions) + "_" + "Update");
        public static readonly ComponentKey ViewChangelogComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(Actions) + "_" + "ViewChangelog");
        public static readonly ComponentKey ViewModPageComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(Actions) + "_" + "ViewModPage");
        public static readonly ComponentKey DeleteItemComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(Actions) + "_" + "DeleteItem");
        public static readonly ComponentKey HideUpdatesComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(Actions) + "_" + "HideUpdates");
        public static string GetColumnHeader() => "Actions";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}

public static class LibraryComponents
{
    
    public sealed class LibraryItemIds : ReactiveR3Object, IItemModelComponent<LibraryItemIds>, IComparable<LibraryItemIds>
    {
        public int CompareTo(LibraryItemIds? other) => 0;

        private readonly OneOf<ObservableHashSet<LibraryItemId>, LibraryItemId[]> _ids;
        private readonly IDisposable? _idsObservable;

        public IEnumerable<LibraryItemId> ItemIds => _ids.Match(
            f0: static x => x.AsEnumerable(),
            f1: static x => x.AsEnumerable()
        );

        public LibraryItemIds(LibraryItemId itemId)
        {
            _ids = new[] { itemId };
        }

        public LibraryItemIds(IObservable<IChangeSet<LibraryItemId, EntityId>> childrenItemIdsObservable)
        {
            _ids = new ObservableHashSet<LibraryItemId>();
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
    
    public sealed class NewVersionAvailable : ReactiveR3Object, IItemModelComponent<NewVersionAvailable>, IComparable<NewVersionAvailable>
    {
        public VersionComponent CurrentVersion { get; }

        private readonly BindableReactiveProperty<string> _newVersion;
        public IReadOnlyBindableReactiveProperty<string> NewVersion => _newVersion;

        private readonly IDisposable _activationDisposable;
        public NewVersionAvailable(VersionComponent currentVersion, string newVersion, Observable<string> newVersionObservable)
        {
            CurrentVersion = currentVersion;
            _newVersion = new BindableReactiveProperty<string>(value: newVersion);

            _activationDisposable = this.WhenActivated(newVersionObservable, static (self, state, disposables) =>
            {
                self.CurrentVersion.Activate().AddTo(disposables);

                var newVersionObservable = state;
                newVersionObservable
                    .Subscribe(self, static (newVersion, self) => self._newVersion.Value = newVersion)
                    .AddTo(disposables);
            });
        }

        public int CompareTo(NewVersionAvailable? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            var oldVersionComparison = string.Compare(CurrentVersion.Value.Value, other.CurrentVersion.Value.Value, StringComparison.OrdinalIgnoreCase);
            if (oldVersionComparison != 0) return oldVersionComparison;
            return string.Compare(NewVersion.Value, other.NewVersion.Value, StringComparison.OrdinalIgnoreCase);
        }

        public FilterResult MatchesFilter(Filter filter)
        {
            return filter switch
            {
                Filter.UpdateAvailableFilter updateFilter => updateFilter.ShowWithUpdates 
                    ? FilterResult.Pass : FilterResult.Fail,
                Filter.VersionFilter versionFilter => 
                    (NewVersion.Value.Contains(versionFilter.VersionPattern, StringComparison.OrdinalIgnoreCase) ||
                     CurrentVersion.Value.Value.Contains(versionFilter.VersionPattern, StringComparison.OrdinalIgnoreCase))
                    ? FilterResult.Pass : FilterResult.Fail,
                Filter.TextFilter textFilter => 
                    (NewVersion.Value.Contains(textFilter.SearchText, 
                        textFilter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) ||
                     CurrentVersion.Value.Value.Contains(textFilter.SearchText, 
                        textFilter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
                    ? FilterResult.Pass : FilterResult.Fail,
                _ => FilterResult.Indeterminate // Default: no opinion
            };
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Disposable.Dispose(_activationDisposable, CurrentVersion);
                }

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }
    
    public sealed class RelatedCollectionsComponent : ReactiveR3Object, IItemModelComponent<RelatedCollectionsComponent>, IComparable<RelatedCollectionsComponent>
    {
        private readonly IDisposable _activationDisposable; 
        
        public StringComponent InstalledCollections { get; }
        public StringComponent DownloadedCollections { get; }

        public ValueComponent<bool> ShowSeparator { get; }
        public ValueComponent<bool> HasInstalledCollections { get; }
        public ValueComponent<bool> HasDownloadedCollections { get; }

        public RelatedCollectionsComponent(IObservable<IChangeSet<string>> installedCollections, IObservable<IChangeSet<string>> downloadedCollections)
        {
            InstalledCollections = new StringComponent("", installedCollections.QueryWhenChanged(items => string.Join(", ", items)));
            DownloadedCollections = new StringComponent("", downloadedCollections.QueryWhenChanged(items => string.Join(", ", items)));
            
            ShowSeparator = new ValueComponent<bool>(false, installedCollections.IsNotEmpty().CombineLatest(downloadedCollections.IsNotEmpty(),
                (hasInstalled, hasDownloaded) => hasDownloaded && hasInstalled));
            HasInstalledCollections = new ValueComponent<bool>(false, installedCollections.IsNotEmpty());
            HasDownloadedCollections = new ValueComponent<bool>(false, downloadedCollections.IsNotEmpty());
            
            _activationDisposable = this.WhenActivated(static (self, disposables) =>
            {
                self.InstalledCollections.Activate().AddTo(disposables);
                self.DownloadedCollections.Activate().AddTo(disposables);
                self.ShowSeparator.Activate().AddTo(disposables);
                self.HasInstalledCollections.Activate().AddTo(disposables);
                self.HasDownloadedCollections.Activate().AddTo(disposables);
            });
        }
        
        public int CompareTo(RelatedCollectionsComponent? other)
        {
            if (other is null) return 1;
            var installedCompare = InstalledCollections.CompareTo(other.InstalledCollections);
            return installedCompare != 0 ? installedCompare : DownloadedCollections.CompareTo(other.DownloadedCollections);
        }

        public FilterResult MatchesFilter(Filter filter)
        {
            var installedResult = InstalledCollections.MatchesFilter(filter);
            if (installedResult == FilterResult.Pass) return FilterResult.Pass;

            var downloadedResult = DownloadedCollections.MatchesFilter(filter);
            if (downloadedResult == FilterResult.Pass) return FilterResult.Pass;

            if (installedResult == FilterResult.Fail || downloadedResult == FilterResult.Fail)
                return FilterResult.Fail;

            return FilterResult.Indeterminate;
        }
        
        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Disposable.Dispose(_activationDisposable, ShowSeparator, InstalledCollections, HasInstalledCollections, HasDownloadedCollections);
                }

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }

    public sealed class InstallAction : ReactiveR3Object, IItemModelComponent<InstallAction>, IComparable<InstallAction>
    {
        public IReadOnlyBindableReactiveProperty<bool> IsInstalled { get; }
        public IReadOnlyBindableReactiveProperty<string> ButtonText { get; }
        public ReactiveCommand<Unit> CommandInstall { get; }

        public int CompareTo(InstallAction? other)
        {
            if (other is null) return 1;
            return IsInstalled.Value.CompareTo(other.IsInstalled.Value);
        }

        public FilterResult MatchesFilter(Filter filter)
        {
            return filter switch
            {
                Filter.InstalledFilter installedFilter => 
                    ((IsInstalled.Value && installedFilter.ShowInstalled) ||
                     (!IsInstalled.Value && installedFilter.ShowNotInstalled))
                    ? FilterResult.Pass : FilterResult.Fail,
                Filter.TextFilter textFilter => ButtonText.Value.Contains(
                    textFilter.SearchText, 
                    textFilter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)
                    ? FilterResult.Pass : FilterResult.Fail,
                _ => FilterResult.Indeterminate // Default: no opinion
            };
        }

        private readonly ReactiveR3Object _source;
        private readonly IDisposable _activationDisposable;

        public InstallAction(
            ValueComponent<bool> isInstalled)
        {
            _source = isInstalled;

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
            ValueComponent<MatchesData> matches)
        {
            _source = matches;

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

            _activationDisposable = this.WhenActivated(static (self, disposables) =>
            {
                self._source.Activate().AddTo(disposables);
            });
        }

        internal static string GetButtonText(bool isInstalled) => isInstalled
            ? Language.LibraryComponents_InstallAction_ButtonText_Installed // "Installed"
            : Language.LibraryComponents_InstallAction_ButtonText_Install; // "Install"

        [SuppressMessage("ReSharper", "RedundantIfElseBlock")]
        [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
        private static string GetButtonText(MatchesData matchesData, bool isExpanded)
        {
            var (numInstalled, numTotal) = matchesData;

            if (numInstalled > 0)
            {
                if (numInstalled == numTotal)
                {
                    return Language.LibraryComponents_InstallAction_ButtonText_Installed;
                } else {
                    return $"{Language.LibraryComponents_InstallAction_ButtonText_Installed} {numInstalled}/{numTotal}";
                }
            }
            else
            {
                if (!isExpanded && numTotal == 1)
                {
                    return Language.LibraryComponents_InstallAction_ButtonText_Install;
                } else {
                    return $"{Language.LibraryComponents_InstallAction_ButtonText_Install} {numTotal}";
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

    public sealed class UpdateAction : ReactiveR3Object, IItemModelComponent<UpdateAction>, IComparable<UpdateAction>
    {
        public ReactiveCommand<Unit> CommandUpdateAndKeepOld { get; } = new();
        public ReactiveCommand<Unit> CommandUpdateAndReplace { get; } = new();

        private readonly BindableReactiveProperty<ModUpdatesOnModPage> _newFiles;
        public IReadOnlyBindableReactiveProperty<ModUpdatesOnModPage> NewFiles => _newFiles;

        private readonly BindableReactiveProperty<string> _buttonText;
        public IReadOnlyBindableReactiveProperty<string> ButtonText => _buttonText;

        private readonly BindableReactiveProperty<string> _updateButtonText;
        public IReadOnlyBindableReactiveProperty<string> UpdateButtonText => _updateButtonText;

        private readonly BindableReactiveProperty<string> _updateAndKeepOldModButtonText;
        public IReadOnlyBindableReactiveProperty<string> UpdateAndKeepOldModButtonText => _updateAndKeepOldModButtonText;

        public int CompareTo(UpdateAction? other)
        {
            if (other is null) return 1;
            return NewFiles.Value.NewestFile().UploadedAt.CompareTo(other.NewFiles.Value.NewestFile().UploadedAt);
        }

        public FilterResult MatchesFilter(Filter filter)
        {
            return filter switch
            {
                Filter.UpdateAvailableFilter updateFilter => updateFilter.ShowWithUpdates
                    ? FilterResult.Pass : FilterResult.Fail,
                Filter.TextFilter textFilter => ButtonText.Value.Contains(
                    textFilter.SearchText, 
                    textFilter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)
                    ? FilterResult.Pass : FilterResult.Fail,
                _ => FilterResult.Indeterminate // Default: no opinion
            };
        }

        private readonly IDisposable _activationDisposable;
        
        // Single mod (row)
        public UpdateAction(
            ModUpdateOnPage initialValue,
            Observable<ModUpdateOnPage> valueObservable)
        {
            var initialUpdates = (ModUpdatesOnModPage)initialValue;
            _newFiles = new BindableReactiveProperty<ModUpdatesOnModPage>(value: initialUpdates);
            _buttonText = new BindableReactiveProperty<string>("");
            _updateButtonText = new BindableReactiveProperty<string>(value: GetUpdateButtonText(initialUpdates.NumberOfModFilesToUpdate));
            _updateAndKeepOldModButtonText = new BindableReactiveProperty<string>(value: GetUpdateAndKeepOldModButtonText(initialUpdates.NumberOfModFilesToUpdate));

            _activationDisposable = this.WhenActivated(valueObservable, static (self, observable, disposables) =>
            {
                observable.Subscribe(self, static (value, self) => 
                {
                    var updates = (ModUpdatesOnModPage)value;
                    self._newFiles.Value = updates;
                    self._updateButtonText.Value = GetUpdateButtonText(updates.NumberOfModFilesToUpdate);
                    self._updateAndKeepOldModButtonText.Value = GetUpdateAndKeepOldModButtonText(updates.NumberOfModFilesToUpdate);
                }).AddTo(disposables);
            });
        }

        // Mod page (row)
        public UpdateAction(
            ModUpdatesOnModPage initialValue,
            Observable<ModUpdatesOnModPage> valuesObservable)
        {
            _newFiles = new BindableReactiveProperty<ModUpdatesOnModPage>(value: initialValue);
            _buttonText = new BindableReactiveProperty<string>(value: GetButtonText(initialValue.NumberOfModFilesToUpdate));
            _updateButtonText = new BindableReactiveProperty<string>(value: GetUpdateButtonText(initialValue.NumberOfModFilesToUpdate));
            _updateAndKeepOldModButtonText = new BindableReactiveProperty<string>(value: GetUpdateAndKeepOldModButtonText(initialValue.NumberOfModFilesToUpdate));

            _activationDisposable = this.WhenActivated(valuesObservable, static (self, observable, disposables) =>
            {
                observable.Subscribe(self, static (values, self) =>
                {
                    self._newFiles.Value = values;
                    self._buttonText.Value = GetButtonText(values.NumberOfModFilesToUpdate);
                    self._updateButtonText.Value = GetUpdateButtonText(values.NumberOfModFilesToUpdate);
                    self._updateAndKeepOldModButtonText.Value = GetUpdateAndKeepOldModButtonText(values.NumberOfModFilesToUpdate);
                }).AddTo(disposables);
            });
        }

        private static string GetButtonText(int numUpdatable)
        {
            // empty string will turn off the buttons label, if more than 1 update then display it
            return numUpdatable <= 1  ? string.Empty : numUpdatable.ToString();
        }

        private static string GetUpdateButtonText(int numUpdatable) => string.Format(Language.Library_Update, numUpdatable);
        private static string GetUpdateAndKeepOldModButtonText(int numUpdatable) => string.Format(Language.Library_UpdateAndKeepOldMod, numUpdatable);

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Disposable.Dispose(_activationDisposable, CommandUpdateAndKeepOld, CommandUpdateAndReplace, 
                        _buttonText, _updateButtonText, _updateAndKeepOldModButtonText);
                }

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }

    public sealed class ViewChangelogAction : ReactiveR3Object, IItemModelComponent<ViewChangelogAction>, IComparable<ViewChangelogAction>
    {
        public ReactiveCommand<Unit> CommandViewChangelog { get; } = new();
        public IReadOnlyBindableReactiveProperty<bool> IsEnabled { get; }

        public int CompareTo(ViewChangelogAction? other)
        {
            if (other is null) return 1;
            return 0; // All view changelog actions are considered equal for sorting
        }

        public ViewChangelogAction(bool isEnabled = true)
        {
            IsEnabled = new BindableReactiveProperty<bool>(isEnabled);
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Disposable.Dispose(CommandViewChangelog, IsEnabled);
                }

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }

    
    public sealed class DeleteItemAction : ReactiveR3Object, IItemModelComponent<DeleteItemAction>, IComparable<DeleteItemAction>
    {
        public ReactiveCommand<Unit> CommandDeleteItem { get; } = new();
        public IReadOnlyBindableReactiveProperty<bool> IsEnabled { get; }

        public int CompareTo(DeleteItemAction? other)
        {
            if (other is null) return 1;
            return 0; // All DeleteItemAction items are considered equal for sorting
        }

        public DeleteItemAction(bool isEnabled = true)
        {
            IsEnabled = new BindableReactiveProperty<bool>(isEnabled);
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Disposable.Dispose(CommandDeleteItem, IsEnabled);
                }

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }

    public sealed class HideUpdatesAction : ReactiveR3Object, IItemModelComponent<HideUpdatesAction>, IComparable<HideUpdatesAction>
    {
        public ReactiveCommand<Unit> CommandHideUpdates { get; } = new();
        public BindableReactiveProperty<bool> IsEnabled { get; }
        public BindableReactiveProperty<bool> IsHidden { get; }
        public BindableReactiveProperty<bool> IsVisible { get; }
        public BindableReactiveProperty<string> ButtonText { get; }
        public BindableReactiveProperty<IconValue> Icon { get; }

        public int CompareTo(HideUpdatesAction? other)
        {
            if (other is null) return 1;
            return 0; // All hide updates actions are considered equal for sorting
        }

        public HideUpdatesAction(Observable<bool> isHiddenObservable, Observable<int> itemCount, Observable<bool> isEnabled, Observable<bool> isVisible)
        {
            IsHidden = isHiddenObservable.ToBindableReactiveProperty();
            IsEnabled = isEnabled.ToBindableReactiveProperty();
            IsVisible = isVisible.ToBindableReactiveProperty();
            
            // Button text changes based on hidden state and item count
            // We use BindableReactiveProperty on ButtonText field as UI elements bind to this.
            ButtonText = IsHidden.AsObservable()
                .CombineLatest(itemCount, static (isHidden, count) => FormatShowUpdates(isHidden, count))
                .ToBindableReactiveProperty(initialValue: FormatShowUpdates(false, 0));

            // Icon changes based on hidden state
            // Use Visibility when hidden (showing "Show Updates"), VisibilityOff when shown (showing "Hide Updates")
            Icon = IsHidden.AsObservable()
                .Select(static isHidden => isHidden ? IconValues.Visibility : IconValues.VisibilityOff)
                .ToBindableReactiveProperty(initialValue: IconValues.Visibility);
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                    Disposable.Dispose(CommandHideUpdates, IsEnabled, IsHidden, IsVisible, ButtonText, Icon);

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }

        private static string FormatShowUpdates(bool isHidden, int itemCount)
        {
            return string.Format(isHidden 
                ? Resources.Language.Library_ShowUpdates 
                : Resources.Language.Library_HideUpdates, itemCount);
        }
    }
}
