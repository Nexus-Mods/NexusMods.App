using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Overlays.Generic.MessageBox.Base;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.Generic.MessageBox.OkCancel;

public partial class MessageBoxOkCancelView : ReactiveUserControl<IMessageBoxOkCancelViewModel>
{
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MessageBoxOkCancelView, string>(nameof(Title), "Cancel and delete download?");
    
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly StyledProperty<string> DescriptionProperty =
        AvaloniaProperty.Register<MessageBoxOkCancelView, string>(nameof(Description), "This is some very long text that spans multiple lines!! This text is super cool!!");
    
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    
    public string Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
    
    public MessageBoxOkCancelView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            OkButton.Command = ReactiveCommand.CreateFromTask(() =>
            {
                ViewModel!.DialogResult = true;
                return Task.CompletedTask;
            });
            
            CancelButton.Command = ReactiveCommand.CreateFromTask(() =>
            {
                ViewModel!.DialogResult = false;
                ViewModel!.IsActive = false;
                return Task.CompletedTask;
            });
            
            CloseButton.Command = ReactiveCommand.CreateFromTask(() =>
            {
                ViewModel!.DialogResult = false;
                ViewModel!.IsActive = false;
                return Task.CompletedTask;
            });
        });
    }
}

