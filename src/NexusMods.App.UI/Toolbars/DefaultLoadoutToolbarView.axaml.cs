using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.App.UI.Toolbars;

public partial class DefaultLoadoutToolbarView : ReactiveUserControl<IDefaultLoadoutToolbarViewModel>
{
    public DefaultLoadoutToolbarView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(x => x.ViewModel!.Caption)
                .BindTo(this, x => x.CaptionText.Text)
                .DisposeWith(d);

            AddModButton.Command = ReactiveCommand.CreateFromTask(AddMod);

        });
    }

    public async Task AddMod()
    {
        var provider = TopLevel.GetTopLevel(this)!.StorageProvider;
        var options =
            new FilePickerOpenOptions
            {
                Title = "Please select a mod file to install.",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("ZIP") {Patterns = new [] {"*.zip"}},
                    new FilePickerFileType("RAR") {Patterns = new [] {"*.rar"}},
                    new FilePickerFileType("7-Zip") {Patterns = new [] {"*.7z"}},
                }
            };

        foreach (var file in await provider.OpenFilePickerAsync(options))
        {
            await ViewModel!.StartManualModInstall(file.Path.LocalPath);
        }
    }
}

