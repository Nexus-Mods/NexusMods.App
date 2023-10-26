# UI Coding Conventions and Guidelines

## Introduction

The NexusMods App uses [Avalonia] combined with [ReactiveUI] and [Dynamic Data]. This document contains conventions, guidelines, tips and tutorials on how to use these tools in the project. We'll also be going over some technical details on how these tools work and how they interact with the project.

## Avalonia

[Avalonia] is a multi-platform UI framework for creating native .NET applications. It takes inspiration from [WPF] and [WinUI] but is distinctly different to ensure it works on all platforms. The framework also uses [Skia] for rendering to ensure cross-platform compatability.

## Understanding XAML Bindings and Reactivity

[Avalonia] uses AXAML, which has some minor differences compared to the standard XAML that was popularized by [WPF]. Creating a new view called `MyView.axaml` will also create a **code behind** file called `MyView.axaml.cs` that, by default, only has a `InitializeComponent();` call inside it's constructor:

```csharp
public partial class MyView : UserControl
{
    public MyView()
    {
        InitializeComponent();
    }
}
```

To actually display any data you need a **data context**. Every control in Avalonia has a property called `DataContext`. You can set this property to some instance of a class that contains data that you want to display:

```csharp
public class MyData
{
    public string Greeting => "Hello World!";
}

public partial class MyView : UserControl
{
    public MyView()
    {
        DataContext = new MyData();

        InitializeComponent();
    }
}
```

Inside the AXAML file of the view, you can now add a `TextBlock` and **bind** the `MyData.Greeting` property to the `TextBlock.Text` property:

```xml
<StackPanel>
    <TextBlock Text="{Binding Greeting}" />
</StackPanel>
```

This kind of binding is called **XAML Binding** because the binding is created inside the UI markup file.

The default **binding mode** for most properties is **one way** meaning that binding is from the source, aka the data context, to the target, aka the view. Currently, the `MyData.Greeting` property is a get-only property, meaning it doesn't have a setter and the underlying field can't be changed. Let's change the `MyData.Getting` property to have a public setter:

```csharp
public class MyData
{
    public string Greeting { get; set; } = "Hello World!";
}
```

To update the text, we can add a simple button to the view that, when triggered, will change the property to something else:

```xml
<StackPanel>
    <TextBlock Text="{Binding Greeting}" />
    <Button Click="OnClick">Change Text</Button>
</StackPanel>
```

```csharp
public partial class MyView : UserControl
{
    public MyView()
    {
        DataContext = new MyData();

        InitializeComponent();
    }

    private void OnClick(object? sender, RoutedEventArgs e)
    {
        DataContext.Greeting = "Hallo Welt!";
    }
}
```

If you were to build and run this project, you'll find that clicking the button does nothing. This is because the data context doesn't **notify** the view that the property has changed. To add this functionality, the data context needs to implement the [`INotifyPropertyChanged`](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifypropertychanged) interface:

```csharp
public class MyData : INotifyPropertyChanged
{
    private string _greeting = "Hello World!";
    public string Greeting
    {
        get => _greeting;
        set
        {
            _greeting = value;
            OnPropertyChanged(nameof(Greeting));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

This is a lot of boilerplate code that various frameworks and tools can abstract away but you should be aware of what's going on behind the scenes. In this code snippet, we explicitly implement the property setter to invoke the [`PropertyChanged`](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifypropertychanged.propertychanged) event.

If the view has any bindings and the data context implements this interface, the view will register an event handler at runtime to listen for property changes of properties that the view binds to. This allows the framework to re-render only a specific part of the UI since it knows which parts of the UI have bindings and which property changes it needs to listen to.

## Understanding ReactiveUI

TODO

---

[Avalonia]: https://docs.avaloniaui.net/
[ReactiveUI]: https://www.reactiveui.net/
[Dynamic Data]: https://dynamic-data.org/
[WPF]: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/
[WinUI]: https://learn.microsoft.com/en-us/windows/apps/winui/
[Skia]: https://skia.org/
