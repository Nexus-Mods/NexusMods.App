using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public partial class PanelView : ReactiveUserControl<IPanelViewModel>
{
    private static byte GetRandomByte()
    {
        var i = Random.Shared.Next(byte.MinValue, byte.MaxValue);
        return (byte)i;
    }

    public PanelView()
    {
        InitializeComponent();

        var color = Color.FromRgb(GetRandomByte(), GetRandomByte(), GetRandomByte());
        Background = new ImmutableSolidColorBrush(color);

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel!.ActualBounds)
                .Subscribe(bounds =>
                {
                    Width = bounds.Width;
                    Height = bounds.Height;
                    Canvas.SetLeft(this, bounds.X);
                    Canvas.SetTop(this, bounds.Y);
                })
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.ClosePanelCommand, view => view.ClosePanelButton)
                .DisposeWith(disposables);
        });
    }
}

