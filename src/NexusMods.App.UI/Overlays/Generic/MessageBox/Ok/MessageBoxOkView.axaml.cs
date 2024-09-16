using Avalonia;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Resources;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;

public partial class MessageBoxOkView : ReactiveUserControl<IMessageBoxOkViewModel>
{
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MessageBoxOkView, string>(nameof(Title), Language.CancelDownloadOverlayView_Title);

    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly StyledProperty<string> DescriptionProperty =
        AvaloniaProperty.Register<MessageBoxOkView, string>(nameof(Description), "This is some very long text that spans multiple lines!! This text is super cool!!");

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

    public MessageBoxOkView()
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
                ViewModel!.Complete(Unit.Default);
            });

            CloseButton.Command = ReactiveCommand.Create(() =>
            {
                ViewModel!.Complete(Unit.Default);
            });
        });
    }
}

