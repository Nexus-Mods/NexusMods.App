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
        this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel, vm => vm.ConfigurablePathsContainer.CurrentValue, v => v.PathsList.ItemsSource)
                    .DisposeWith(d);
            }
        );
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
                            var newPaths = ViewModel!.ConfigurablePathsContainer.CurrentValue.Prepend(new ConfigurablePath(null, localPath)).Distinct();
                            ViewModel.ConfigurablePathsContainer.CurrentValue = newPaths.ToArray();
                        }
                    );
                }
            }
        );
    }

    private void Remove_OnClick(object? sender, RoutedEventArgs e)
    {
        var button = (Button)sender!;
        if (button.DataContext is not ConfigurablePath path)
        {
            return;
        }
        var newPaths = ViewModel!.ConfigurablePathsContainer.CurrentValue.Except([path]).ToArray();
        ViewModel.ConfigurablePathsContainer.CurrentValue = newPaths;
    }
}

