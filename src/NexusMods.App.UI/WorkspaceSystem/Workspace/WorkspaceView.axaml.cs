using System.Diagnostics;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

[UsedImplicitly]
public partial class WorkspaceView : ReactiveUserControl<IWorkspaceViewModel>
{
    public WorkspaceView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            Debug.Assert(WorkspaceCanvas.Children.Count == 0);

            ViewModel!.Panels
                .ToObservableChangeSet()
                .SubscribeWithErrorLogging(changeSet =>
                {
                    // TODO: this is stupid, find a better way

                    foreach (var change in changeSet)
                    {
                        Console.WriteLine(change.Reason);

                        switch (change.Reason)
                        {
                            case ListChangeReason.Add:
                            {
                                var item = change.Item.Current;
                                WorkspaceCanvas.Children.Add(CreateView(item));
                                break;
                            }
                            case ListChangeReason.AddRange:
                            {
                                var items = change.Range;
                                WorkspaceCanvas.Children.AddRange(items.Select(CreateView));
                                break;
                            }
                            case ListChangeReason.Remove:
                            {
                                var item = change.Item.Current;
                                var existingControl = WorkspaceCanvas.Children.FirstOrDefault(x =>
                                {
                                    if (x is not PanelView panelView) return false;
                                    return panelView.ViewModel?.Id == item.Id;
                                });

                                if (existingControl is null) return;
                                WorkspaceCanvas.Children.Remove(existingControl);
                                break;
                            }
                            default: throw new NotSupportedException();
                        }
                    }
                })
                .DisposeWith(disposables);
        });
    }

    private static Control CreateView(IPanelViewModel panelViewModel)
    {
        return new PanelView
        {
            ViewModel = panelViewModel
        };
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Console.WriteLine(nameof(ArrangeOverride));
        ViewModel?.ArrangePanels(finalSize);
        return base.ArrangeOverride(finalSize);
    }
}
