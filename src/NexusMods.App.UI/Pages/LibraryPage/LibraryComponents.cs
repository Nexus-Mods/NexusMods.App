using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.Networking.NexusWebApi;
using ObservableCollections;
using OneOf;
using R3;
using ReactiveUI;
using Disposable = R3.Disposable;

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
            var aInstall = a.GetOptional<LibraryComponents.InstallAction>(key: InstallComponentKey);
            var bInstall = b.GetOptional<LibraryComponents.InstallAction>(key: InstallComponentKey);

            if (aInstall.HasValue && bInstall.HasValue) return aInstall.Value.CompareTo(bInstall.Value);
            if (aInstall.HasValue) return -1;
            if (bInstall.HasValue) return 1;

            var aUpdate = a.GetOptional<LibraryComponents.UpdateAction>(key: UpdateComponentKey);
            var bUpdate = b.GetOptional<LibraryComponents.UpdateAction>(key: UpdateComponentKey);
            if (aUpdate.HasValue && bUpdate.HasValue) return aUpdate.Value.CompareTo(bUpdate.Value);

            return 0;
        }

        public const string ColumnTemplateResourceKey = nameof(LibraryColumns) + "_" + nameof(Actions);

        public static readonly ComponentKey InstallComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(Actions) + "_" + "Install");
        public static readonly ComponentKey UpdateComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(Actions) + "_" + "Update");
        public static string GetColumnHeader() => "Actions";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}

public static class LibraryComponents
{
    public sealed class NewVersionAvailable : ReactiveR3Object, IItemModelComponent<NewVersionAvailable>, IComparable<NewVersionAvailable>
    {
        public StringComponent CurrentVersion { get; }

        private readonly BindableReactiveProperty<string> _newVersion;
        public IReadOnlyBindableReactiveProperty<string> NewVersion => _newVersion;

        private readonly IDisposable _activationDisposable;
        public NewVersionAvailable(StringComponent currentVersion, string newVersion, Observable<string> newVersionObservable)
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
        private readonly IDisposable? _idsObservable;

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

            _activationDisposable = this.WhenActivated(static (self, disposables) =>
            {
                self._source.Activate().AddTo(disposables);
            });

            _idsObservable = childrenItemIdsObservable.SubscribeWithErrorLogging(changeSet => _ids.AsT0.ApplyChanges(changeSet));
        }

        internal static string GetButtonText(bool isInstalled) => isInstalled ? "Installed" : "Install";

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
                    Disposable.Dispose(_activationDisposable, _idsObservable ?? Disposable.Empty, CommandInstall, ButtonText, _source);
                }

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }

    public sealed class UpdateAction : ReactiveR3Object, IItemModelComponent<UpdateAction>, IComparable<UpdateAction>
    {
        public ReactiveCommand<Unit> CommandUpdate { get; } = new();

        private readonly BindableReactiveProperty<NexusModsFileMetadata.ReadOnly> _newFile;
        public IReadOnlyBindableReactiveProperty<NexusModsFileMetadata.ReadOnly> NewFile => _newFile;

        private readonly BindableReactiveProperty<string> _buttonText;
        public IReadOnlyBindableReactiveProperty<string> ButtonText => _buttonText;

        public int CompareTo(UpdateAction? other)
        {
            if (other is null) return 1;
            return NewFile.Value.UploadedAt.CompareTo(other.NewFile.Value.UploadedAt);
        }

        private readonly IDisposable _activationDisposable;
        
        // Single mod (row)
        public UpdateAction(
            NexusModsFileMetadata.ReadOnly initialValue,
            Observable<NexusModsFileMetadata.ReadOnly> valueObservable)
        {
            _newFile = new BindableReactiveProperty<NexusModsFileMetadata.ReadOnly>(value: initialValue);
            _buttonText = new BindableReactiveProperty<string>(value: Resources.Language.LibraryItemButtonUpdate_Single);

            _activationDisposable = this.WhenActivated(valueObservable, static (self, observable, disposables) =>
            {
                observable.Subscribe(self, static (value, self) => self._newFile.Value = value).AddTo(disposables);
            });
        }

        // Mod page (row)
        public UpdateAction(
            NewerFilesOnModPage initialValue,
            Observable<NewerFilesOnModPage> valuesObservable)
        {
            _newFile = new BindableReactiveProperty<NexusModsFileMetadata.ReadOnly>(value: initialValue.NewestFile());
            _buttonText = new BindableReactiveProperty<string>(value: GetButtonText(initialValue.Files.Length));

            _activationDisposable = this.WhenActivated(valuesObservable, static (self, observable, disposables) =>
            {
                observable.Subscribe(self, static (values, self) =>
                {
                    self._newFile.Value = values.NewestFile();
                    self._buttonText.Value = GetButtonText(values.Files.Length);
                }).AddTo(disposables);
            });
        }

        private static string GetButtonText(int numUpdatable)
        {
            // Note(sewer): These strings in the comments below are accurate, just temporarily changed
            // as we're shipping 'phase one' for the SDV release. Do not edit.

            // 'Update ({0})'
            return string.Format(Resources.Language.LibraryItemButtonUpdate_CounterInBracket, numUpdatable);
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Disposable.Dispose(_activationDisposable, CommandUpdate);
                }

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
