using Avalonia;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.Generic.MessageBox.OkCancel;

public partial class MessageBoxOkCancelView : ReactiveUserControl<IMessageBoxOkCancelViewModel>
{
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MessageBoxOkCancelView, string>(nameof(Title), "Something has happened!");

    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly StyledProperty<string> DescriptionProperty =
        AvaloniaProperty.Register<MessageBoxOkCancelView, string>(nameof(Description), "This is some very long text that spans multiple lines!! This text is super cool!! This text is super cool!! This text is super cool!!This text is super cool!!This text is super cool!!This text is super cool!!");

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

        // Bind the View's properties to the UI elements
        this.WhenAnyValue(x => x.Title)
            .BindTo(this, x => x.HeadingText.Text);
            
        this.WhenAnyValue(x => x.Description)
            .BindTo(this, x => x.MessageTextBlock.Text);
        
        this.WhenActivated(_ =>
        {
            OkButton.Command = ReactiveCommand.Create(() =>
            {
                ViewModel!.Complete(true);
            });

            CancelButton.Command = ReactiveCommand.Create(() =>
            {
                ViewModel!.Complete(false);
            });

            CloseButton.Command = ReactiveCommand.Create(() =>
            {
                ViewModel!.Complete(false);
            });
        });
    }
}

