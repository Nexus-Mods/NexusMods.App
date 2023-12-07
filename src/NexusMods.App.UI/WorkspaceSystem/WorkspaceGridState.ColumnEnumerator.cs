using System.Collections.Immutable;

namespace NexusMods.App.UI.WorkspaceSystem;

public readonly partial struct WorkspaceGridState
{
    public readonly record struct ColumnInfo(double X, double Width)
    {
        public double Right() => X + Width;
    }

    public ref struct Column
    {
        public readonly ColumnInfo Info;
        public readonly ReadOnlySpan<PanelGridState> Rows;

        public Column(ColumnInfo info, ReadOnlySpan<PanelGridState> rows)
        {
            Info = info;
            Rows = rows;
        }
    }

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

            var slice = rowBuffer[..rowCount];
            slice.Sort(YComparer.Instance);

            Current = new Column(columnInfo, rowBuffer[..rowCount]);

            return true;
        }

        private void Setup()
        {
            for (var i = 0; i < _seenColumns.Length; i++)
            {
                _seenColumns[i] = new ColumnInfo(double.PositiveInfinity, double.PositiveInfinity);
            }

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
