using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using NexusMods.App.UI.Resources;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Banners;

[PseudoClasses(":dismissed")]
public class InfoBanner : TemplatedControl
{
    public static readonly StyledProperty<IconValue> IconProperty = AvaloniaProperty.Register<InfoBanner, IconValue>(nameof(Icon), defaultValue: IconValues.LiveHelp);

    public static readonly StyledProperty<string> PrefixProperty = AvaloniaProperty.Register<InfoBanner, string>(nameof(Prefix), defaultValue: Language.InfoBanner_PrefixProperty_Did_you_know_);

    public static readonly StyledProperty<object?> ContentProperty = AvaloniaProperty.Register<InfoBanner, object?>(nameof(Content));

    public static readonly StyledProperty<ReactiveCommand<Unit, BannerSettingsWrapper>> DismissCommandProperty = AvaloniaProperty.Register<InfoBanner, ReactiveCommand<Unit, BannerSettingsWrapper>>(nameof(DismissCommand));

    public static readonly StyledProperty<BannerSettingsWrapper?> BannerSettingsWrapperProperty = AvaloniaProperty.Register<InfoBanner, BannerSettingsWrapper?>(nameof(BannerSettingsWrapper));

    public IconValue Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Prefix
    {
        get => GetValue(PrefixProperty);
        set => SetValue(PrefixProperty, value);
    }

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public ReactiveCommand<Unit, BannerSettingsWrapper> DismissCommand
    {
        get => GetValue(DismissCommandProperty);
        private set => SetValue(DismissCommandProperty, value);
    }

    public BannerSettingsWrapper? BannerSettingsWrapper
    {
        get => GetValue(BannerSettingsWrapperProperty);
        set => SetValue(BannerSettingsWrapperProperty, value);
    }

    public InfoBanner()
    {
        // Start out dismissed
        UpdatePseudoClasses(isDismissed: true);

        var canDismiss = this.WhenAnyValue(x => x.BannerSettingsWrapper).Select(x => x is not null);
        DismissCommand = ReactiveCommand.Create(() =>
        {
            UpdatePseudoClasses(isDismissed: true);
            BannerSettingsWrapper!.DismissBanner();

            return BannerSettingsWrapper;
        }, canDismiss);
    }

    private readonly SerialDisposable _serialDisposable = new();
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == BannerSettingsWrapperProperty)
        {
            _serialDisposable.Disposable = null;

            if (change.NewValue is BannerSettingsWrapper bannerSettingsWrapper)
            {
                UpdatePseudoClasses(bannerSettingsWrapper.IsDismissed);

                _serialDisposable.Disposable = bannerSettingsWrapper
                    .WhenAnyValue(x => x.IsDismissed)
                    .Subscribe(UpdatePseudoClasses);
            }
        }

        base.OnPropertyChanged(change);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        _serialDisposable.Disposable = null;
        base.OnUnloaded(e);
    }

    private void UpdatePseudoClasses(bool isDismissed)
    {
        if (isDismissed) PseudoClasses.Add(":dismissed");
        else PseudoClasses.Remove(":dismissed");
    }
}
