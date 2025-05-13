using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public partial class CustomContentView : ReactiveUserControl<IDialogContentViewModel>
{
    public CustomContentView()
    {
        InitializeComponent();
    }
}
