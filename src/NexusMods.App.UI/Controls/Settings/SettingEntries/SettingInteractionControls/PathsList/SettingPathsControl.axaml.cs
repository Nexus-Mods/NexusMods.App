using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using NexusMods.Abstractions.Settings;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries.PathsList;

public partial class SettingPathsControl : ReactiveUserControl<ISettingPathsViewModel>
{
    public SettingPathsControl()
    {
        InitializeComponent();
    }

    private void Add_OnClick(object? sender, RoutedEventArgs e)
    {
        Task.Run(async () =>
            {
                var topLevel = TopLevel.GetTopLevel(this);

                var path = await topLevel!.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
                    {
                        AllowMultiple = false,
                        Title = "Select a folder",
                    }
                );

                if (path.Count == 1)
                {
                    var localPath = path[0].TryGetLocalPath();
                    if (localPath is null)
                    {
                        return;
                    }

                    Dispatcher.UIThread.Invoke(() =>
                        {
                            // we only ever have one path in the list, so we can just replace it
                            ViewModel!.ConfigurablePathsContainer.CurrentValue = [new ConfigurablePath(null, localPath)];
                        }
                    );
                }
            }
        );
    }
}
