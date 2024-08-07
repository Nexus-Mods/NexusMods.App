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

        GoBackCommand = ReactiveCommand.Create(() => TraverseHistory(offset: -1), canGoBack);

        var canGoForward = this.WhenAnyValue(
            vm => vm.Position,
            vm => vm.Head).Select(_ => Position < Head);

        GoForwardCommand = ReactiveCommand.Create(() => TraverseHistory(offset: +1), canGoForward);
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

    public void AddToHistory(PageData pageData)
    {
        if (IsTraversing) return;
        if (pageData.Context.IsEphemeral) return;

        var next = Position + 1;

        var index = next % Limit;
        _history[index] = pageData;

        Position = next;
        Head = next;
    }
}
