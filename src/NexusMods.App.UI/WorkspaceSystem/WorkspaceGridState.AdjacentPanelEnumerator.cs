using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace NexusMods.App.UI.WorkspaceSystem;

public readonly partial struct WorkspaceGridState
{
    [Flags]
    public enum AdjacencyKind : byte
    {
        None = 0,
        SameRow = 1 << 0,
        SameColumn = 1 << 1
    }

    public record struct AdjacentPanel(PanelGridState Panel, AdjacencyKind Kind);

    public struct AdjacentPanelEnumerator : IEnumerator<AdjacentPanel>
    {
        private ImmutableSortedSet<PanelGridState>.Enumerator _enumerator;
        private readonly PanelGridState _anchor;
        private readonly bool _includeAnchor;

        internal AdjacentPanelEnumerator(WorkspaceGridState parent, PanelGridState anchor, bool includeAnchor)
        {
            _enumerator = parent.GetEnumerator();
            _anchor = anchor;
            _includeAnchor = includeAnchor;
        }

        public AdjacentPanelEnumerator GetEnumerator() => this;

        public AdjacentPanel Current { get; private set; }
        [SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            while (_enumerator.MoveNext())
            {
                var other = _enumerator.Current;
                if (!_includeAnchor && other.Id == _anchor.Id) continue;

                var (anchorRect, otherRect) = (_anchor.Rect, other.Rect);
                var flags = AdjacencyKind.None;

                // same column
                // | a | x |  | b | x |
                // | b | x |  | a | x |
                if (otherRect.Left.IsGreaterThanOrCloseTo(anchorRect.Left) && otherRect.Right.IsLessThanOrCloseTo(anchorRect.Right))
                {
                    if (otherRect.Top.IsCloseTo(anchorRect.Bottom) || otherRect.Bottom.IsCloseTo(anchorRect.Top))
                    {
                        flags |= AdjacencyKind.SameColumn;
                    }
                }

                // same row
                // | a | b |  | b | a |  | a | b |
                // | x | x |  | x | x |  | a | c |
                if (otherRect.Top.IsGreaterThanOrCloseTo(anchorRect.Top) && otherRect.Bottom.IsLessThanOrCloseTo(anchorRect.Bottom))
                {
                    if (otherRect.Left.IsCloseTo(anchorRect.Right) || otherRect.Right.IsCloseTo(anchorRect.Left))
                    {
                        flags |= AdjacencyKind.SameRow;
                    }
                }

                if (flags == AdjacencyKind.None) continue;

                Current = new AdjacentPanel(other, flags);
                return true;
            }

            return false;
        }

        public void Reset() => _enumerator.Reset();

        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}
