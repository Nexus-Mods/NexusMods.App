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
    /// Arbitrarly choosen limit.
    /// </summary>
    private const int Limit = 10;

    /// <summary>
    /// Underlying array containing page data.
    /// </summary>
    private readonly PageData[] _history = new PageData[Limit];

    /// <summary>
    /// Points to the current place in history.
    /// </summary>
    [Reactive] private int ReadPointer { get; set; } = -1;

    /// <summary>
    /// Points to the next place in the history that will be written to.
    /// </summary>
    [Reactive] private int WritePointer { get; set; }

    /// <summary>
    /// Gets the amount of items in the history.
    /// </summary>
    /// <remarks>
    /// The ring buffer has a limit, once that limit is reached
    /// previous items will be overwritten. As such, the count
    /// can never be above the limit.
    /// </remarks>
    private int Count => Math.Min(WritePointer, Limit);

    /// <summary>
    /// Intermediate value used when traversing the history to prevent
    /// traversing from triggering adding to the history.
    /// </summary>
    private bool IsTraversing { get; set; }

    public ReactiveCommand<Unit, Unit> GoBackCommand { get; }
    public ReactiveCommand<Unit, Unit> GoForwardCommand { get; }

    public TabHistory(Action<PageData> openPageFunc)
    {
        var canGoBack = this.WhenAnyValue(
            vm => vm.ReadPointer,
            vm => vm.WritePointer).Select(_ =>
        {
            var start = WritePointer - Count;
            return start < ReadPointer;
        });

        GoBackCommand = ReactiveCommand.Create(() =>
        {
            var next = ReadPointer - 1;
            var index = next % Limit;
            var pageData = _history[index];

            ReadPointer = next;

            try
            {
                IsTraversing = true;
                openPageFunc(pageData);
            }
            finally
            {
                IsTraversing = false;
            }
        }, canGoBack);

        var canGoForward = this.WhenAnyValue(
            vm => vm.ReadPointer,
            vm => vm.WritePointer).Select(_ => ReadPointer < WritePointer - 1);

        GoForwardCommand = ReactiveCommand.Create(() =>
        {
            var next = ReadPointer + 1;
            var index = next % Limit;
            var pageData = _history[index];

            ReadPointer = next;

            try
            {
                IsTraversing = true;
                openPageFunc(pageData);
            }
            finally
            {
                IsTraversing = false;
            }
        }, canGoForward);
    }

    public void AddToHistory(PageData pageData)
    {
        if (IsTraversing) return;
        if (pageData.Context.IsEphemeral) return;

        var next = ReadPointer + 1;

        var index = next % Limit;
        _history[index] = pageData;

        WritePointer = next + 1;
        ReadPointer = next;
    }
}
