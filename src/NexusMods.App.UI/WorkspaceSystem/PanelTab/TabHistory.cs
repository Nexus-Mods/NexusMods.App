using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

/// <summary>
/// The tab history is a linear history of non-ephemeral pages implemented using a ring buffer.
/// </summary>
internal class TabHistory : ReactiveObject
{

    /// <summary>
    /// Arbitrarily chosen limit.
    /// </summary>
    private const int Limit = 10;

    /// <summary>
    /// Underlying array containing page data.
    /// </summary>
    private readonly PageData[] _history = new PageData[Limit];

    /// <summary>
    /// Points to the tail, the last position.
    /// </summary>
    private int Tail => Math.Max(0, Head - Math.Min(Limit, Head));

    /// <summary>
    /// Points to the head, the first position.
    /// </summary>
    [Reactive] private int Head { get; set; } = -1;

    /// <summary>
    /// Points to the current position in the history.
    /// </summary>
    [Reactive] private int Position { get; set; } = -1;

    /// <summary>
    /// Intermediate value used when traversing the history to prevent
    /// traversing from triggering adding to the history.
    /// </summary>
    private bool IsTraversing { get; set; }

    public ReactiveCommand<Unit, Unit> GoBackCommand { get; }
    public ReactiveCommand<Unit, Unit> GoForwardCommand { get; }

    private readonly Action<PageData> _openPageFunc;
    public TabHistory(Action<PageData> openPageFunc)
    {
        _openPageFunc = openPageFunc;

        var canGoBack = this.WhenAnyValue(
            vm => vm.Position,
            vm => vm.Head).Select(_ => Position > Tail);

        GoBackCommand = ReactiveCommand.Create(() => TraverseHistory(offset: _isEphemeralPage ? 0 : -1), canGoBack);

        var canGoForward = this.WhenAnyValue(
            vm => vm.Position,
            vm => vm.Head).Select(_ => Position < Head);

        GoForwardCommand = ReactiveCommand.Create(() => TraverseHistory(offset: _isEphemeralPage ? 0 : +1), canGoForward);
    }

    private void TraverseHistory(int offset)
    {
        var next = Position + offset;
        var index = next % Limit;
        var pageData = _history[index];

        Position = next;

        try
        {
            IsTraversing = true;
            _openPageFunc(pageData);
        }
        finally
        {
            IsTraversing = false;
        }
    }

    /// <summary>
    /// Whether the current page displayed is an ephemeral page, and thus not
    /// in the history.
    /// </summary>
    private bool _isEphemeralPage;

    public void AddToHistory(PageData pageData)
    {
        _isEphemeralPage = pageData.Context.IsEphemeral;
        if (IsTraversing) return;

        // NOTE(erri120): ephemeral pages don't get added to the
        // history, but we still want to move the head to prevent
        // going forwards.
        if (_isEphemeralPage)
        {
            Head = Position;
            return;
        }

        var next = Position + 1;

        var index = next % Limit;
        _history[index] = pageData;

        Position = next;
        Head = next;
    }
}
