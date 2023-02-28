using Avalonia;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Helpers;

public class InjectedDataContext : AvaloniaObject
{
    public static IServiceProvider? ServiceProvider { get; set; }
    
    static InjectedDataContext()
    {
        ViewModelProperty.Changed.Subscribe(x => HandleChange(x.Sender, x.NewValue.GetValueOrDefault<Type>()));
    }

    private static void HandleChange(IAvaloniaObject objSender, Type getValueOrDefault)
    {
        if (ServiceProvider == null) return;
        
        if (objSender is IViewFor vf)
            vf.ViewModel = ServiceProvider.GetService(getValueOrDefault);
    }
    

    public static readonly AttachedProperty<Type> ViewModelProperty = AvaloniaProperty.RegisterAttached<IAvaloniaObject, Type>("ViewModel", typeof(InjectedDataContext));


    public static Type GetViewModel(IAvaloniaObject obj)
    {
        return (Type)obj.GetValue(ViewModelProperty);
    }

    public static void SetViewModel(IAvaloniaObject obj, Type value)
    {
        obj.SetValue(ViewModelProperty, value);
    }
}