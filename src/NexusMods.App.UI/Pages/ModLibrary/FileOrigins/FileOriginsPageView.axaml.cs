using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Resources;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ModLibrary;

public partial class FileOriginsPageView : ReactiveUserControl<IFileOriginsPageViewModel>
{
    public FileOriginsPageView()
    {
        InitializeComponent();
        
        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel,
                    vm => vm.FileOrigins,
                    v => v.DataGrid.ItemsSource)
                .DisposeWith(d);

            DataGrid.Width = Double.NaN;
            
            AddModButton.Command = ReactiveCommand.CreateFromTask(AddMod);
            AddModAdvancedButton.Command = ReactiveCommand.CreateFromTask(AddModAdvanced);
        });
    }
    
    private async Task AddMod()
    {
        foreach (var file in await PickModFiles())
        {
            await ViewModel!.AddMod(file.Path.LocalPath);
        }
    }

    private async Task AddModAdvanced()
    {
        foreach (var file in await PickModFiles())
        {
            await ViewModel!.AddModAdvanced(file.Path.LocalPath);
        }
    }

    private async Task<IEnumerable<IStorageFile>> PickModFiles()
    {
        var provider = TopLevel.GetTopLevel(this)!.StorageProvider;
        var options =
            new FilePickerOpenOptions
            {
                Title = Language.LoadoutGridView_AddMod_FilePicker_Title,
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(Language.LoadoutGridView_AddMod_FileType_Archive) {Patterns = new [] {"*.zip", "*.7z", "*.rar"}},
                }
            };

        return await provider.OpenFilePickerAsync(options);
    }
}

