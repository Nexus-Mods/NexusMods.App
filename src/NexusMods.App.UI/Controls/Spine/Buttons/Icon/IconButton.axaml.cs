using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Icon;

public partial class IconButton : ReactiveUserControl<IIconButtonViewModel>, IViewContract
{
    public static readonly StyledProperty<IconType> IconProperty =
        AvaloniaProperty.Register<IconButton, IconType>(nameof(Icon));

    public string ViewContract
    {
        get => GetValue(IconProperty).ToString();
        set
        {
            var parsed = Enum.Parse<IconType>(value);
            SetValue(IconProperty, parsed);
            RestClasses(parsed);
        }
    }

    public IconType Icon
    {
        get => GetValue(IconProperty);
        set
        {
            SetValue(IconProperty, value);
            RestClasses(value);
        }
    }

    public enum IconType
    {
        Home,
        Add,
        Download
    }

    public IconButton()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel
                .WhenAnyValue(vm => vm.IsActive)
                .StartWith(false)
                .SubscribeWithErrorLogging(logger: default, SetClasses)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.Click, v => v.Button)
                .DisposeWith(disposables);
        });
    }

    private void SetClasses(bool isActive)
    {
        if (isActive)
        {
            Button.Classes.Add("Active");
            Button.Classes.Remove("Inactive");
        }
        else
        {
            Button.Classes.Remove("Active");
            Button.Classes.Add("Inactive");
        }
    }


    private void RestClasses(IconType value)
    {
        foreach (var type in Enum.GetValues<IconType>())
        {
            if (type == value) continue;
            Button.Classes.Remove(type.ToString());
        }
        Button.Classes.Add(value.ToString());
    }

}
