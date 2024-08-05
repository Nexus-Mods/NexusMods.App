using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelTabViewModel : AViewModel<IPanelTabViewModel>, IPanelTabViewModel
{
    /// <inheritdoc/>
    public PanelTabId Id { get; } = PanelTabId.From(Guid.NewGuid());

    /// <inheritdoc/>
    public IPanelTabHeaderViewModel Header { get; }

    /// <inheritdoc/>
    [Reactive] public required Page Contents { get; set; }

    /// <inheritdoc/>
    [Reactive] public bool IsVisible { get; set; } = true;

    /// <inheritdoc/>
    public ReactiveCommand<Unit, Unit> GoBackInHistoryCommand { get; }

    /// <inheritdoc/>
    public ReactiveCommand<Unit, Unit> GoForwardInHistoryCommand { get; }

    private const int HistoryLimit = 10;
    private readonly PageData[] _history = new PageData[HistoryLimit];
    [Reactive] private int ReadIndex { get; set; } = -1;
    [Reactive] private int WriteIndex { get; set; }
    [Reactive] private int HistoryCount { get; set; }
    private bool IsAdvancing { get; set; }

    public PanelTabViewModel(IWorkspaceController workspaceController, WorkspaceId workspaceId, PanelId panelId)
    {
        Header = new PanelTabHeaderViewModel(Id);

        var canGoBack = this.WhenAnyValue(
            vm => vm.ReadIndex,
            vm => vm.HistoryCount,
            vm => vm.WriteIndex).Select(_ =>
        {
            var start = WriteIndex - HistoryCount;
            return start < ReadIndex;
        });

        GoBackInHistoryCommand = ReactiveCommand.Create(() =>
        {
            var next = ReadIndex - 1;
            var index = next % HistoryLimit;
            var pageData = _history[index];

            ReadIndex = next;

            try
            {
                IsAdvancing = true;
                OpenPage(pageData);
            }
            finally
            {
                IsAdvancing = false;
            }
        }, canGoBack);

        var canGoForward = this.WhenAnyValue(
            vm => vm.ReadIndex,
            vm => vm.WriteIndex).Select(_ => ReadIndex < WriteIndex - 1);

        GoForwardInHistoryCommand = ReactiveCommand.Create(() =>
        {
            var next = ReadIndex + 1;
            var index = next % HistoryLimit;
            var pageData = _history[index];

            ReadIndex = next;

            try
            {
                IsAdvancing = true;
                OpenPage(pageData);
            }
            finally
            {
                IsAdvancing = false;
            }
        }, canGoForward);

        this.WhenAnyValue(vm => vm.Contents)
            .Where(_ => !IsAdvancing)
            .WhereNotNull()
            .Select(contents => contents.PageData)
            .SubscribeWithErrorLogging(pageData =>
            {
                var next = ReadIndex + 1;

                var index = next % HistoryLimit;
                _history[index] = pageData;

                WriteIndex = next + 1;
                ReadIndex = next;

                HistoryCount = Math.Min(WriteIndex, HistoryLimit);
            });

        return;

        void OpenPage(PageData pageData)
        {
            workspaceController.OpenPage(workspaceId, pageData, behavior: new OpenPageBehavior(new OpenPageBehavior.ReplaceTab(panelId, Header.Id)), selectTab: true, checkOtherPanels: false);
        }
    }

    public TabData? ToData()
    {
        if (Contents.PageData.Context.IsEphemeral) return null;

        return new TabData
        {
            Id = Id,
            PageData = Contents.PageData,
        };
    }
}
