using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public partial class CustomContentView : ReactiveUserControl<IViewModelInterface>
{
    public CustomContentView()
    {
        InitializeComponent();
    }
}
