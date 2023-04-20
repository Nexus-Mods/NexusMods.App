using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace NexusMods.App.UI.RightContent.Downloads;

public partial class HistoryView : ReactiveUserControl<IHistoryViewModel>
{
    public HistoryView()
    {
        InitializeComponent();
    }
}

