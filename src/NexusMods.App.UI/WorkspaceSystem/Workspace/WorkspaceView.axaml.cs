using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public partial class WorkspaceView : ReactiveUserControl<IWorkspaceViewModel>
{
    public WorkspaceView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, vm => vm.AddPanelCommand, view => view.AddPanelButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.RemovePanelCommand, view => view.RemovePanelButton)
                .DisposeWith(disposables);

            ViewModel!.Panels
                .ToObservableChangeSet()
                .SubscribeWithErrorLogging(changeSet =>
                {
                    // TODO: this is stupid, find a better way

                    foreach (var change in changeSet)
                    {
                        Console.WriteLine(change.Reason);

                        if (change.Reason == ListChangeReason.Add)
                        {
                            var item = change.Item.Current;
                            WorkspaceCanvas.Children.Add(CreateViewModelHost(item));
                        } else if (change.Reason == ListChangeReason.Remove)
                        {
                            var item = change.Item.Current;
                            var existingControl = WorkspaceCanvas.Children.FirstOrDefault(x =>
                            {
                                if (x is not ViewModelViewHost viewModelViewHost) return false;
                                if (viewModelViewHost.ViewModel is not IPanelViewModel panelViewModel) return false;
                                return panelViewModel.Id == item.Id;
                            });

                            if (existingControl is null) return;
                            WorkspaceCanvas.Children.Remove(existingControl);
                        } else if (change.Reason == ListChangeReason.AddRange)
                        {
                            var items = change.Range;
                            WorkspaceCanvas.Children.AddRange(items.Select(CreateViewModelHost));
                        }
                    }
                })
                .DisposeWith(disposables);
        });
    }

    private static Control CreateViewModelHost(IPanelViewModel panelViewModel)
    {
        var host = new ViewModelViewHost
        {
            ViewModel = panelViewModel
        };

        return host;
    }
}
