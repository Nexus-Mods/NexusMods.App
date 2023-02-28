using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.UI.Theme;

public class MainWindowViewModel
{
    public MainWindowViewModel()
    {
        HierarchicalData = new HierarchicalTreeDataGridSource<FileWrapper>(new FileWrapper(KnownFolders.EntryFolder.TopParent, false));
        HierarchicalData.Columns.AddRange(FileWrapper.Columns);

        TableData = KnownFolders.EntryFolder.TopParent.EnumerateFiles(recursive: false)
            .Select(f => new DataRow(f.FileName.ToString(), (long)f.Length.Value, f.LastWriteTime))
            .ToArray();
    }
    
    public HierarchicalTreeDataGridSource<FileWrapper> HierarchicalData { get; }
    
    public IEnumerable<DataRow> TableData { get; }

    public record FileWrapper(AbsolutePath Path, bool IsFile)
    {
        public static IColumn<FileWrapper>[] Columns => new IColumn<FileWrapper>[]
        {
            new HierarchicalExpanderColumn<FileWrapper>(new TextColumn<FileWrapper, string>("Name", x => x.Path.FileName.ToString()),
                x => x.Path.EnumerateFiles(recursive:false)
                        .Select(xi => new FileWrapper(xi, true))
                    .Concat(x.Path.EnumerateDirectories(recursive:false)
                        .Select(y => new FileWrapper(y, false))),
                x => !x.IsFile),
            new TextColumn<FileWrapper,Size?>("Size", x => x.IsFile ? x.Path.Length : null),
            new TextColumn<FileWrapper,DateTime>("Last Modified", x => x.Path.LastWriteTime)
        }; 
    }

    public record DataRow(string Name, long? Size, DateTime LastModified);
}