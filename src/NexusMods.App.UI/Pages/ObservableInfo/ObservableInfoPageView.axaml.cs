using System.Reactive.Disposables;
using System.Text;
using Avalonia.ReactiveUI;
using DynamicData.Kernel;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ObservableInfo;

public partial class ObservableInfoPageView : ReactiveUserControl<IObservableInfoPageViewModel>
{
    public ObservableInfoPageView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.TrackingStates, view => view.States.ItemsSource)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.TrackingStates.Count, view => view.Count.Text, static i => $"Observable Count: {i}")
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Series, view => view.PieChart.Series)
                .DisposeWith(disposables);

            this.Bind(
                ViewModel,
                vm => vm.SelectedItem.Value,
                view => view.States.SelectedItem,
                vmToViewConverter: static optional => optional.ValueOrDefault(),
                viewToVmConverter: static value =>
                {
                    if (value is TrackingState trackingState) return trackingState;
                    return Optional<TrackingState>.None;
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.SelectedItem.Value)
                .SubscribeWithErrorLogging(optional =>
                {
                    if (!optional.HasValue)
                    {
                        SelectedItemInfo.IsVisible = false;
                        return;
                    }

                    var state = optional.Value;
                    SelectedItemInfo.IsVisible = true;
                    TextType.Text = $"Type: {state.FormattedType}";
                    TextDate.Text = $"Add Time: {state.AddTime:U}";
                    TextStackTraceFull.Text = state.StackTrace;

                    var sb = new StringBuilder();

                    var span = state.StackTrace.AsSpan();
                    var ranges = span.Split('\n');

                    foreach (var range in ranges)
                    {
                        var line = span[range];
                        var trimmed = line.TrimStart();

                        if (!trimmed.StartsWith("at NexusMods", StringComparison.OrdinalIgnoreCase))
                            continue;

                        sb.AppendLine(line.ToString());
                    }

                    TextStackTraceSlim.Text = sb.ToString();
                });
        });
    }
}
