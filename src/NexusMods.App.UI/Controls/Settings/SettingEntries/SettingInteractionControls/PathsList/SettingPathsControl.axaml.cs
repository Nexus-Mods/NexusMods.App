using Avalonia.Controls;
using Avalonia.ReactiveUI;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries.PathsList;

public partial class SettingPathsControl : ReactiveUserControl<ISettingPathsViewModel>
{
    public SettingPathsControl()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storageProvider is not null)
            {
                this.WhenAnyValue(view => view.ViewModel)
                    .WhereNotNull()
                    .SubscribeWithErrorLogging(vm => vm.StorageProvider = storageProvider)
                    .AddTo(disposables);
            }

            this.BindCommand(ViewModel, vm => vm.CommandChangeLocation, view => view.ButtonChangeLocation)
                .AddTo(disposables);
        });
    }
}
