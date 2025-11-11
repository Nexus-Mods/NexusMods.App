using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Downloads;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.TreeDataGrid;
using OneOf;
using R3;

namespace NexusMods.App.UI.Pages.Downloads;

public readonly record struct PauseMessage(DownloadInfo[] Downloads);
public readonly record struct ResumeMessage(DownloadInfo[] Downloads);
public readonly record struct CancelMessage(DownloadInfo[] Downloads);

public sealed class DownloadsTreeDataGridAdapter(IServiceProvider serviceProvider,IDownloadsDataProvider provider, DownloadsFilter filter) :
    TreeDataGridAdapter<CompositeItemModel<DownloadId>, DownloadId>(serviceProvider),
    ITreeDataGirdMessageAdapter<OneOf<PauseMessage, ResumeMessage, CancelMessage>>
{
    public Subject<OneOf<PauseMessage, ResumeMessage, CancelMessage>> MessageSubject { get; } = new();

    protected override IObservable<IChangeSet<CompositeItemModel<DownloadId>, DownloadId>> GetRootsObservable(bool viewHierarchical)
    {
        return provider.ObserveDownloads(filter);
    }

    protected override IColumn<CompositeItemModel<DownloadId>>[] CreateColumns(bool viewHierarchical)
    {
        var nameColumn = ColumnCreator.Create<DownloadId, DownloadColumns.Name>();

        return
        [
            viewHierarchical ? ITreeDataGridItemModel<CompositeItemModel<DownloadId>, DownloadId>.CreateExpanderColumn(nameColumn) : nameColumn,
            ColumnCreator.Create<DownloadId, DownloadColumns.Game>(),
            ColumnCreator.Create<DownloadId, DownloadColumns.Size>(),
            ColumnCreator.Create<DownloadId, DownloadColumns.Speed>(),
            ColumnCreator.Create<DownloadId, DownloadColumns.Status>(),
        ];
    }

    protected override void BeforeModelActivationHook(CompositeItemModel<DownloadId> model)
    {
        base.BeforeModelActivationHook(model);

        model.SubscribeToComponentAndTrack<DownloadComponents.StatusComponent, DownloadsTreeDataGridAdapter>(
            key: DownloadColumns.Status.ComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.PauseCommand.Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, model, _) = state;
                var downloads = GetDownloads(model);
                self.MessageSubject.OnNext(new PauseMessage(downloads));
            })
        );

        model.SubscribeToComponentAndTrack<DownloadComponents.StatusComponent, DownloadsTreeDataGridAdapter>(
            key: DownloadColumns.Status.ComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.ResumeCommand.Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, model, _) = state;
                var downloads = GetDownloads(model);
                self.MessageSubject.OnNext(new ResumeMessage(downloads));
            })
        );

        model.SubscribeToComponentAndTrack<DownloadComponents.StatusComponent, DownloadsTreeDataGridAdapter>(
            key: DownloadColumns.Status.ComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CancelCommand.Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, model, _) = state;
                var downloads = GetDownloads(model);
                self.MessageSubject.OnNext(new CancelMessage(downloads));
            })
        );
    }

    private static DownloadInfo[] GetDownloads(CompositeItemModel<DownloadId> model)
    {
        var downloadRef = model.GetOptional<DownloadRef>(DownloadColumns.DownloadRefComponentKey);
        if (downloadRef.HasValue)
            return [downloadRef.Value.Download];
        
        return [];
    }
}
