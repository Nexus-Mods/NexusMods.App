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
}
