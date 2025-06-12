using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Platform.Storage;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.CrossPlatform.Process;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Disposable = R3.Disposable;
using ReactiveCommand = R3.ReactiveCommand;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries.PathsList;

public class SettingPathsViewModel : AViewModel<ISettingPathsViewModel>, ISettingPathsViewModel
{
    public IValueContainer ValueContainer => ConfigurablePathsContainer;
    public ConfigurablePathsContainer ConfigurablePathsContainer { get; }
    [Reactive] public bool HasChanged { get; private set; }

    public IStorageProvider? StorageProvider { get; set; }
    public ReactiveCommand CommandChangeLocation { get; }

    public SettingPathsViewModel(IOSInterop osInterop, ConfigurablePathsContainer pathsContainer)
    {
        ConfigurablePathsContainer = pathsContainer;

        CommandChangeLocation = new ReactiveCommand(
            executeAsync: async (_, cancellationToken) =>
            {
                var paths = await StorageProvider!.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = "Select a folder",
                });

                if (paths.Count != 1) return;
                var localPath = paths[0].TryGetLocalPath();
                if (localPath is null) return;

                ConfigurablePathsContainer.CurrentValue = [new ConfigurablePath(null, localPath)];
            }, awaitOperation: AwaitOperation.Drop, configureAwait: false, maxSequential: 1
        );

        this.WhenActivated(disposables =>
        {
            Disposable.Create(this, static vm => vm.StorageProvider = null).AddTo(disposables);

            ValueContainer.WhenAnyValue(x => x.HasChanged)
                .BindToVM(this, vm => vm.HasChanged)
                .DisposeWith(disposables);
        });
    }
}
