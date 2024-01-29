using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Resources;
using ReactiveUI;
using static NexusMods.App.UI.Controls.DataGrid.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid;

public partial class LoadoutGridView : ReactiveUserControl<ILoadoutGridViewModel>
{
    public LoadoutGridView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {

            this.WhenAnyValue(view => view.ViewModel!.Mods)
                .BindToUi(this, view => view.ModsDataGrid.ItemsSource)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.LoadoutName)
                .BindToUi(this, view => view.ModlistNameText.Text)
                .DisposeWith(d);

            AddModButton.Command =
                ReactiveCommand.CreateFromTask(AddMod);

            AddModAdvancedButton.Command =
                ReactiveCommand.CreateFromTask(AddModAdvanced);

            DeleteModsButton.Command =
                ReactiveCommand.CreateFromTask(DeleteSelectedMods);

            this.WhenAnyValue(view => view.ViewModel!.Columns)
                .GenerateColumns(ModsDataGrid)
                .DisposeWith(d);
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

    private async Task DeleteSelectedMods()
    {
        var toDelete = new List<ModId>();
        foreach (var row in ModsDataGrid.SelectedItems)
        {
            if (row is not ModCursor modCursor) continue;
            toDelete.Add(modCursor.ModId);
        }
        await ViewModel!.DeleteMods(toDelete, "Deleted by user via UI.");
    }
}

