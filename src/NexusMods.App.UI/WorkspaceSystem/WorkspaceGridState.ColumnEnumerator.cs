using System.Collections.Immutable;
using System.Text;

namespace NexusMods.App.UI.WorkspaceSystem;

public readonly partial struct WorkspaceGridState
{
    public readonly record struct ColumnInfo(double X, double Width)
    {
        public double Right() => X + Width;
    }

    public readonly ref struct Column(ColumnInfo info, ReadOnlySpan<PanelGridState> rows)
    {
        public readonly ColumnInfo Info = info;
        public readonly ReadOnlySpan<PanelGridState> Rows = rows;

        public string ToDebugString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Info: {Info.ToString()}");
            foreach (var row in Rows)
            {
                sb.AppendLine(row.ToString());
            }

            sb.AppendLine();
            return sb.ToString();
        }
    }

    /// <summary>
    /// Efficient column enumerator.
    /// </summary>
    public ref struct ColumnEnumerator
    {
        private ImmutableSortedSet<PanelGridState>.Enumerator _enumerator;

        private readonly Span<ColumnInfo> _seenColumns;
        private int _numColumns;
        private int _currentColumnIndex;

        public ColumnEnumerator(WorkspaceGridState parent, Span<ColumnInfo> seenColumns)
        {
            _enumerator = parent.GetEnumerator();
            _seenColumns = seenColumns;

            Setup();
        }

        public Column Current { get; private set; }

        public bool MoveNext(Span<PanelGridState> rowBuffer)
        {
            if (_currentColumnIndex == _numColumns) return false;
            var columnInfo = _seenColumns[_currentColumnIndex++];

            var rowCount = 0;
            while (_enumerator.MoveNext())
            {
                var current = _enumerator.Current;
                var rect = current.Rect;

                if (rect.X.IsCloseTo(columnInfo.X) && rect.Right.IsGreaterThanOrCloseTo(columnInfo.Right()))
                {
                    rowBuffer[rowCount++] = current;
                } else if (rect.Right.IsCloseTo(columnInfo.Right()) && rect.X.IsLessThanOrCloseTo(columnInfo.X))
                {
                    rowBuffer[rowCount++] = current;
                }
            }

            if (_currentColumnIndex != _numColumns) _enumerator.Reset();

            // sort the rows by the Y component
            var slice = rowBuffer[..rowCount];
            slice.Sort(YComparer.Instance);

            Current = new Column(columnInfo, rowBuffer[..rowCount]);
            return true;
        }

        private void Setup()
        {
            // Fill the Span with positive infinity values
            // This eliminates the need for a running variable and allows us to use BinarySearch
            _seenColumns.Fill(new ColumnInfo(double.PositiveInfinity, double.PositiveInfinity));

            while (_enumerator.MoveNext())
            {
                var (_, rect) = _enumerator.Current;
                var info = new ColumnInfo(rect.X, rect.Width);

                if (_numColumns == 0)
                {
                    _seenColumns[_numColumns++] = info;
                }
                else
                {
                    var index = _seenColumns.BinarySearch(info, XComparer.Instance);
                    if (index < 0)
                    {
                        _seenColumns[~index] = info;
                        _numColumns += 1;
                    }
                    else
                    {
                        // the binary search only uses the X component, we want the smallest columns only
                        var other = _seenColumns[index];
                        if (other.Width > rect.Width)
                        {
                            _seenColumns[index] = info;
                        }
                    }
                }
            }

            _enumerator.Reset();
        }

        public void Dispose() => _enumerator.Dispose();

        private class XComparer : IComparer<ColumnInfo>
        {
            public static readonly IComparer<ColumnInfo> Instance = new XComparer();
            public int Compare(ColumnInfo a, ColumnInfo b)
            {
                return a.X.CompareTo(b.X);
            }
        }

        private class YComparer : IComparer<PanelGridState>
        {
            public static readonly IComparer<PanelGridState> Instance = new YComparer();

            public int Compare(PanelGridState a, PanelGridState b)
            {
                return a.Rect.Y.CompareTo(b.Rect.Y);
            }
        }
    }
}
