using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.Paths;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public class LoadoutViewModel : APageViewModel<ILoadoutViewModel>, ILoadoutViewModel
{
    private readonly IConnection _connection;

    [Reactive] public ITreeDataGridSource<LoadoutItemModel>? Source { get; set; }
    private readonly ObservableCollectionExtended<LoadoutItemModel> _itemModels = [];

    public R3.ReactiveCommand<R3.Unit> SwitchViewCommand { get; }
    [Reactive] public bool ViewHierarchical { get; set; } = true;

    public LoadoutViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, LoadoutId loadoutId) : base(windowManager)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
        var dataProviders = serviceProvider.GetServices<ILibraryDataProvider>().ToArray();

        SwitchViewCommand = new R3.ReactiveCommand<R3.Unit>(_ =>
        {
            ViewHierarchical = !ViewHierarchical;
        });

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.ViewHierarchical)
                .Select(viewHierarchical =>
                {
                    _itemModels.Clear();

                    return ObserveFlatLoadoutItems()
                        .DisposeMany()
                        .OnUI()
                        .Bind(_itemModels);
                })
                .Switch()
                .Select(_ => CreateSource(_itemModels, createHierarchicalSource: ViewHierarchical))
                .BindTo(this, vm => vm.Source)
                .AddTo(disposables);
        });
    }

    private IObservable<IChangeSet<LoadoutItemModel>> ObserveFlatLoadoutItems()
    {
        return LibraryLinkedLoadoutItem
            .ObserveAll(_connection)
            .Transform(loadoutItem =>
            {
                var observable = LibraryLinkedLoadoutItem
                    .Observe(_connection, loadoutItem.Id)
                    .Publish()
                    .AutoConnect();

                var nameObservable = observable.Select(static item => item.AsLoadoutItem().Name);
                var isEnabledObservable = observable.Select(static item => !item.AsLoadoutItem().IsDisabled);

                // TODO: version
                // TODO: size (probably with RevisionsWithChildUpdates)

                return new LoadoutItemModel
                {
                    InstalledAt = loadoutItem.GetCreatedAt(),
                    Name = loadoutItem.AsLoadoutItem().Name,

                    NameObservable = nameObservable,
                    IsEnabledObservable = isEnabledObservable,
                };
            });
    }

    private static ITreeDataGridSource<LoadoutItemModel> CreateSource(IEnumerable<LoadoutItemModel> models, bool createHierarchicalSource)
    {
        if (createHierarchicalSource)
        {
            var source = new HierarchicalTreeDataGridSource<LoadoutItemModel>(models);
            AddColumns(source.Columns, viewAsTree: true);
            return source;
        }
        else
        {
            var source = new FlatTreeDataGridSource<LoadoutItemModel>(models);
            AddColumns(source.Columns, viewAsTree: false);
            return source;
        }
    }

    private static void AddColumns(ColumnList<LoadoutItemModel> columnList, bool viewAsTree)
    {
        var nameColumn = LoadoutItemModel.CreateNameColumn();
        columnList.Add(viewAsTree ? LoadoutItemModel.CreateExpanderColumn(nameColumn) : nameColumn);
        columnList.Add(LoadoutItemModel.CreateVersionColumn());
        columnList.Add(LoadoutItemModel.CreateSizeColumn());
        columnList.Add(LoadoutItemModel.CreateInstalledAtColumn());
        columnList.Add(LoadoutItemModel.CreateToggleEnableColumn());
    }
}
