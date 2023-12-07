using System.Collections.Immutable;

namespace NexusMods.App.UI.WorkspaceSystem;

public readonly partial struct WorkspaceGridState
{
    public readonly record struct RowInfo(double Y, double Height)
    {
        public double Bottom() => Y + Height;
    }

    public ref struct Row
    {
        public readonly RowInfo Info;
        public readonly ReadOnlySpan<PanelGridState> Columns;

        public Row(RowInfo info, ReadOnlySpan<PanelGridState> columns)
        {
            Info = info;
            Columns = columns;
        }
    }

    /// <summary>
    /// Efficient row enumerator.
    /// </summary>
    public ref struct RowEnumerator
    {
        private ImmutableSortedSet<PanelGridState>.Enumerator _enumerator;

        private readonly Span<RowInfo> _seenRows;
        private int _numRows;
        private int _currentRowIndex;

        public RowEnumerator(WorkspaceGridState parent, Span<RowInfo> seenRows)
        {
            _enumerator = parent.GetEnumerator();
            _seenRows = seenRows;

            Setup();
        }

        public Row Current { get; private set; }

        public bool MoveNext(Span<PanelGridState> columnBuffer)
        {
            if (_currentRowIndex == _numRows) return false;
            var rowInfo = _seenRows[_currentRowIndex++];

            var columnCount = 0;
            while (_enumerator.MoveNext())
            {
                var current = _enumerator.Current;
                var rect = current.Rect;

                if (rect.Y.IsCloseTo(rowInfo.Y) && rect.Bottom.IsGreaterThanOrCloseTo(rowInfo.Bottom()))
                {
                    columnBuffer[columnCount++] = current;
                } else if (rect.Bottom.IsCloseTo(rowInfo.Bottom()) && rect.Y.IsLessThanOrCloseTo(rowInfo.Y))
                {
                    columnBuffer[columnCount++] = current;
                }
            }

            if (_currentRowIndex != _numRows) _enumerator.Reset();

            // sort the columns by the X component
            var slice = columnBuffer[..columnCount];
            slice.Sort(XComparer.Instance);

            Current = new Row(rowInfo, columnBuffer[..columnCount]);
            return true;
        }

        private void Setup()
        {
            // Fill the Span with positive infinity values
            // This eliminates the need for a running variable and allows us to use BinarySearch
            _seenRows.Fill(new RowInfo(double.PositiveInfinity, double.PositiveInfinity));

            while (_enumerator.MoveNext())
            {
                var (_, rect) = _enumerator.Current;
                var info = new RowInfo(rect.Y, rect.Height);

                if (_numRows == 0)
                {
                    _seenRows[_numRows++] = info;
                }
                else
                {
                    var index = _seenRows.BinarySearch(info, YComparer.Instance);
                    if (index < 0)
                    {
                        _seenRows[~index] = info;
                        _numRows += 1;
                    }
                    else
                    {
                        // the binary search only uses the Y component, we want the smallest rows only
                        var other = _seenRows[index];
                        if (other.Height > rect.Height)
                        {
                            _seenRows[index] = info;
                        }
                    }
                }
            }

            _enumerator.Reset();
        }

        public void Dispose() => _enumerator.Dispose();

        private class YComparer : IComparer<RowInfo>
        {
            public static readonly IComparer<RowInfo> Instance = new YComparer();
            public int Compare(RowInfo a, RowInfo b)
            {
                return a.Y.CompareTo(b.Y);
            }
        }

        private class XComparer : IComparer<PanelGridState>
        {
            public static readonly IComparer<PanelGridState> Instance = new XComparer();

            public int Compare(PanelGridState a, PanelGridState b)
            {
                return a.Rect.X.CompareTo(b.Rect.X);
            }
        }
    }
}
