using Avalonia;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;
using NexusMods.Icons;

namespace NexusMods.App.UI.Controls;

[PseudoClasses(":active", ":icon")]
public class EmptyState : TemplatedControl
{
    public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<EmptyState, bool>(nameof(IsActive));

    public static readonly StyledProperty<IconValue?> IconProperty = AvaloniaProperty.Register<EmptyState, IconValue?>(nameof(Icon));

    public static readonly StyledProperty<string> HeaderProperty = AvaloniaProperty.Register<EmptyState, string>(nameof(Header), defaultValue: string.Empty);

    public static readonly StyledProperty<object?> SubtitleProperty = AvaloniaProperty.Register<EmptyState, object?>(nameof(Subtitle));

    public static readonly StyledProperty<object?> ContentProperty = AvaloniaProperty.Register<EmptyState, object?>(nameof(Content));

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public IconValue? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public object? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == IsActiveProperty)
        {
            PseudoClasses.Remove(":active");

            if (change.NewValue is true)
            {
                PseudoClasses.Add(":active");
            }
        } else if (change.Property == IconProperty)
        {
            PseudoClasses.Remove(":icon");

            if (change.NewValue is IconValue)
            {
                PseudoClasses.Add(":icon");
            }
        }

        base.OnPropertyChanged(change);
    }
}
