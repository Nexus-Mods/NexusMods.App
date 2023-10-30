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

This kind of binding is called **XAML Binding** because the binding is created inside the UI markup file. The default **binding mode** for most properties is **one way** meaning that binding is from the source, aka the data context, to the target, aka the view.

Another common binding mode is **two way** binding which is required for properties on input controls, like `TextBox.Text` and `Checkbox.IsChecked`.

Note: when using XAML bindings, it's recommended to set the design data context. This provides a hint to the IDE auto-completion service and allows you to use the previewer:

```xml
<Design.DataContext>
    <ui:MyData/>
</Design.DataContext>
```

Currently, the `MyData.Greeting` property is a get-only property, meaning it doesn't have a setter and the underlying field can't be changed. Let's change the `MyData.Getting` property to have a public setter:

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

The `Button` control has a `Click` event that we can use to register an event handler called `OnClick`. This event handler will be called whenever the button is clicked:

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

The XAML bindings from the previous example work great for very simple applications. However, once you start adding more and more functionality to it, developing with XAML bindings can have some massive disadvantages.

The main disadvantage comes from using events. By design, events require you to register an event handler that will receive the sender object and some event arguments. Since the event handler is often just a member function, it's tied to the class and it can be hard to reason about what actually happens when a property changes.

An alternative to events are **observables**. The [observable design pattern](https://learn.microsoft.com/en-us/dotnet/standard/events/observer-design-pattern) is suitable for any scenario that requires push-based notification. The pattern defines an **observable** as a provider for push-based notifications and an **observer** as a mechanism for receiving push-based notifications.

.NET provides an [`IObservable<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.iobservable-1) and an [`IObserver<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.iobserver-1) interface to facilitate this pattern. The great thing about this pattern and how it's implemented in C#, is that it doesn't require any special syntax or keywords like the `event` keyword. Conceptually, these are just interfaces that have methods that return values.

The result of having this property is the ability to use other language constructs like LINQ on the pattern:

```csharp
this.WhenAnyValue(x => x.Text)
    .Select(text => text.Length > 10)
    .Subscribe(hasMinLength => { })
    .DisposeWith(disposables);
```

All of your favorite LINQ methods like `Select`, `Where`, `First`, `All` and more can be used together with `IObservable<T>`. This makes the pattern inherently **composable** and is one of the major reasons why ReactiveUI is commonly used in UI development.

Sadly, ReactiveUI requires a lot of boilerplate code to get started, as well as a base level understanding of various software development concepts like the observable pattern. Another important pattern is the **Model-View-ViewModel** pattern, or **MVVM**. At it's core, the MVVM pattern allows us to separate our various components. The View is what appears on the screen of the user, in the previous example that's `MyView`. The View Model was `MyData`, it provides public properties and commands that the View can bind to. It also facilitates the communication between the View and the "Model" where the model is often a stateful object or some data store.

The MVVM pattern and it's separation of concerns makes developing the views and view models straightforward: the View should only bind to the View Model, the View Model should only contain the functionality required to drive the View and display the data. ReactiveUI makes this pattern much easier to implement.

Let's re-create the previous example and change it to use ReactiveUI and the observable pattern instead of being event-driven with XAML bindings:

We can start with `MyViewModel` and a simple `Greeting` property:

```csharp
public class MyViewModel
{
    public string Greeting { get; } = "Hello World!";
}
```

Instead of creating a normal Avalonia `UserControl`, we can create a `ReactiveUserControl` that is provided the `Avalonia.ReactiveUI` package:

```xml
<reactive:ReactiveUserControl
    x:TypeArguments="ui:MyViewModel"
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:ui="clr-namespace:Example"
    xmlns:reactive="http://reactiveui.net"
    mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
    x:Class="Example.MyView">

    <Design.DataContext>
        <ui:MyViewModel />
    </Design.DataContext>

    <StackPanel>
        <TextBlock />
    </StackPanel>

</reactive:ReactiveUserControl>
```

The code-behind class `MyView.axaml.cs` will also have inherit from `ReactiveUserControl<TViewModel>`:

```csharp
public partial class MyView : ReactiveUserControl<MyViewModel>
{
    public MyView()
    {
        InitializeComponent();
    }
}
```

`ReactiveUserControl<TViewModel>` inherits from the Avalonia `UserControl` class but also implements the ReactiveUI interface `IViewFor<TViewModel>`. This interface extends `IActivatableView`, which is a marker interface for telling ReactiveUI that the current view can be activated. The activation method is an implementation detail of the UI framework itself, ReactiveUI supports more than Avalonia, so this needs to be kept vague. In the context of Avalonia, activation occurs when the view gets loaded: https://github.com/AvaloniaUI/Avalonia/blob/2a85f7cafed6c90d4a8cd11dee36a9dd15ebcc1e/src/Avalonia.ReactiveUI/AvaloniaActivationForViewFetcher.cs#L37-L52

The `Loaded` and `Unloaded` events of an Avalonia `Control` determine whether a view is activated or not. The code snippet above also showcases that you can construct observables from events and thus convert from event-driven programming to the observable pattern.

An activatable view has a lifetime associated with it. Resources should be allocated and bindings should be created when the view gets activated and they should be **disposed** when the view gets deactivated. This is done via the built-in `IDisposable` interface of .NET. This simple interface has only one method:

```csharp
public interface IDisposable
{
    void Dispose();
}
```

The `IObservable<T>` and `IObserver<T>` interfaces are tightly combined with the `IDisposable` interface. The `Subscribe` method on an `IObservable<T>` returns an instance of `IDisposable`. Calling `Dispose` on this returned instance allows the observer to stop receiving notifications before the provider has finished sending them. The important aspect of this is that the observable will remove the reference to the observer, which allows the GC to cleanup the observer object. If you don't dispose the observer or the observable, those references might exist for a longer period of time or even for the entire lifetime of the process which can lead to memory leaks. The only time when you don't need to dispose a subscription, is when you subscribe to yourself.

In summary, instances of `IObservable<T>` and `IObserver<T>` should always be disposed when they go **out of scope**.

Going back to the code-behind of our view, we currently have no bindings at all:

```csharp
public MyView()
{
    InitializeComponent();
}
```

To create a reactive binding, we need to be able to reference the control in our code. This can be done by simply adding a name of the control. In this example, we want to change the text of the `TextBlock`, so we can attach a name to that control in the view:

```xml
<StackPanel>
    <TextBlock x:Name="MyTextBlock" />
</StackPanel>
```

Avalonia has a source generator that will automatically generate a field in the code-behind with the same name:

```csharp
// <auto-generated />
partial class MyView
{
    internal global::Avalonia.Controls.TextBlock MyTextBlock;

    public void InitializeComponent(bool loadXaml = true)
    {
        if (loadXaml)
        {
            AvaloniaXamlLoader.Load(this);
        }

        MyTextBlock = this.FindNameScope()?.Find<global::Avalonia.Controls.TextBlock>("MyTextBlock");
    }
}
```

This is also why the code-behind class has to be `partial` and why the constructor always calls the `InitializeComponent` method. With the name in place, we can create a one-way bind from the `Greeting` property in the View Model to the `Text` property of the `TextBlock` control in our View:

```csharp
public MyView()
{
    InitializeComponent();

    this.WhenActivated(disposables =>
    {
        this.OneWayBind(ViewModel, vm => vm.Greeting, view => view.MyTextBlock.Text)
            .DisposeWith(disposables);
    });
}
```

The `WhenActivated` method is an extension method, which is why we need to call `this.WhenActivated`:

```csharp
public static IDisposable WhenActivated(this IActivatableView item, Action<CompositeDisposable> block, IViewFor? view = null);
```

Importantly, there are multiple overloads of this method, so your IDE might complain until you've finished writing your code. The `disposables` parameter is an instance of `CompositeDisposable`, which represents a group of disposable resources that are disposed together.

Once again, `OneWayBind` is an extension method, so we need to use `this.OneWayBind`:

```csharp
public static IReactiveBinding<TView, TVProp> OneWayBind<TViewModel, TView, TVMProp, TVProp>(
    this TView view,
    TViewModel? viewModel,
    Expression<Func<TViewModel, TVMProp?>> vmProperty,
    Expression<Func<TView, TVProp>> viewProperty,
    object? conversionHint = null,
    IBindingTypeConverter? vmToViewConverterOverride = null)
    where TViewModel : class
    where TView : class, IViewFor);
```

The first actual argument after the current instance of the view is the instance of the View Model that we want to bind to. `ReactiveUserControl<TViewModel>` has a property `TViewModel ViewModel` that we can use for this.

Next is the property on the View Model that we want to select. Notice that this an **expression**. `Expression<T>` is different from `Func<T>` in that they return an [expression tree](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/expression-trees/). This topic is very complex and you can read the docs if you're interested but in summary, expressions are limited and some language features like the null propagating operator `?` can't be used. The reason we use expressions for bindings is because the expression tree allows the framework to **know** which property is being referenced at runtime. Instead of seeing this as a lambda that gets executed, think of it as you telling the framework which property you want to bind to, as such you also don't need to care about the value being null because you only mention the property, you don't inspect it's value. This will be explaind in more detailed in a later section.

After you've selected the View Model property, the same thing has to be done but for the View property. In our case, that's `MyTextBlock.Text`.

The last two arguments of the `OneWayBind` function are optional, they mostly exist to convert between one type to another but should only rarely be used.

```csharp
this.WhenActivated(disposables =>
{
    this.OneWayBind(ViewModel, vm => vm.Greeting, view => view.MyTextBlock.Text)
        .DisposeWith(disposables);
});
```

`OneWayBind` returns an instance of `IReactiveBinding<TView, TValue>` which implements `IDisposable`. As discussed before, this binding has to be disposed when the view gets deactivated. The `DisposeWith` extension method will add the disposable of the binding to the `CompositeDisposable` and guarantees that it will be disposed when the view gets deactivated.

### Commands

As before, the `MyViewModel.Greeting` property is currently just get-only, which means it will never change. Let's change this and add a `Button` that will modify the property:

```csharp
public class MyViewModel
{
    public string Greeting { get; set; } = "Hello World!";
}
```

```xml
<StackPanel>
    <TextBlock x:Name="MyTextBlock" />
    <Button x:Name="MyButton">Click Me!</Button>
</StackPanel>
```

Instead of using the `Click` event as we did before, we make use of another ReactiveUI feature called **Commands**. These allow the View to trigger logic defined in the View Model:

```csharp
public class MyViewModel
{
    public string Greeting { get; set; } = "Hello World!";

    public ReactiveCommand<Unit, Unit> ChangeGreetingCommand { get; }

    public MyViewModel()
    {
        ChangeGreetingCommand = ReactiveCommand.Create(() =>
        {
            Greeting = "Hallo Welt!";
        });
    }
}
```

It's important to note that `ReactiveCommand<TParam, TResult>` takes in a `TParam` and returns a `TResult`. Most commands you'll create won't have any inputs and outputs. As such, you can use the `System.Reactive.Unit` type which essentially represents `void`. You can see this in action when you look at the `ReactiveCommand.Create` method that is specific for a `ReactiveCommand<Unit, Unit>` and requires a method that has no parameters and does not return a value.

Binding this command to the View can be done using the `BindCommand` extension method:

```csharp
this.WhenActivated(disposables =>
{
    this.OneWayBind(ViewModel, vm => vm.Greeting, view => view.MyTextBlock.Text)
        .DisposeWith(disposables);

    this.BindCommand(ViewModel, vm => vm.ChangeGreetingCommand, view => view.MyButton)
        .DisposeWith(disposables);
});
```

This behaves and looks very similar to the previous `OneWayBind` method, however the core difference is that we don't select a property on the control but the control itself. The framework will then figure out how to bind the command to the control for us.

Compiling the application, running this example and clicking the button will not change the text. As with XAML bindings, we need to notify the view that a property has changed. However, instead of manually implementing `INotifyPropertyChanged`, we can use the provided `ReactiveObject` class:

```csharp
public class MyViewModel : ReactiveObject
{
    private string _greeting = "Hello World!";
    public string Greeting
    {
        get => _greeting;
        set => this.RaiseAndSetIfChanged(ref _greeting, value);
    }

    public ReactiveCommand<Unit, Unit> ChangeGreetingCommand { get; }

    public MyViewModel()
    {
        ChangeGreetingCommand = ReactiveCommand.Create(() =>
        {
            Greeting = "Hallo Welt!";
        });
    }
}
```

The `ReactiveUI.Fody` package can be used to simplify the property:

```csharp
[Reactive]
public string { get; set; } = "Hello World!";
```

This will result in the same code but it's much easier to write.

Finally, let's look at another really cool feature of a `ReactiveCommand`, which is the `canExecute` observable:

```csharp
public class MyViewModel : ReactiveObject
{
    [Reactive]
    public string Greeting { get; set; } = "Hello World!";

    public ReactiveCommand<Unit, Unit> ChangeGreetingCommand { get; }

    public MyViewModel()
    {
        var canExecute = this
            .WhenAnyValue(vm => vm.Greeting)
            .Select(greeting => greeting == "Hello World!");

        ChangeGreetingCommand = ReactiveCommand.Create(() =>
        {
            Greeting = "Hallo Welt!";
        }, canExecute);
    }
}
```

When creating a `ReactiveCommand`, you can pass an `IObservable<bool>` along. When the command gets created, it will subscribe to this observable and make the command unavailable if the observable returns `false`. If you bind this command to a `Button`, the framework will disable the button if the command can't execute. This is all done behind the scenes with the `BindCommand` method and one of the reasons why ReactiveUI is so powerful.

Note that `ReactiveCommand<TParam, TResult>` implements `IDisposable` to dispose of the subscription to the `canExecute` observable. If the command doesn't have this observable or the observable only references the current scope, the command doesn't have to be disposed as all references will be removed up once the GC cleans up the scope.

### Understanding expression chains

As previously mentioned, ReactiveUI uses expression trees for methods like `OneWayBind` and `WhenAnyValue`. At runtime, these expressions are partially rewritten to be simpler. Another benefit of using expressions is being able to set up "chains" for nested properties:

```csharp
this.WhenAnyValue(x => x.Foo!.Bar!.Baz)
    .Subscribe(x => Console.WriteLine(x));
```

The first thing to note is that there is no null propagation because expressions don't support this feature yet. This means that you won't be able to use the `?` chaining operator: `x => x.Foo?.Bar?.Baz`.

Secondly, `WhenAny` and all it's variants, will only send notifications if evaluating the expression wouldn't throw a `NullReferenceException`. This is why you can use `x => x.Foo!.Bar!.Baz` to tell the compiler to ignore the nullability of the properties. ReactiveUI will prevent any null related exceptions and crashes from occuring in these expression chains.

Thirdly, and this one is **important**: `x => x.Foo.Bar.Baz` will be set up the following subscriptions:

1) Subscribe to `this`, look for `Foo`
2) Subscribe to `Foo`, look for `Bar`
3) Subscribe to `Bar`, look for `Baz`
4) Subscribe to `Baz`, publish to Subject

- If `Foo` changes, `this` will be notified and it will re-subscribe to the new `Foo`.
- If `Bar` changes, `Foo` will be notified and it will re-subscribe to the new `Bar`.

As such, you don't have to manually manage nested subscriptions, the framework does it for you.

Lastly, `WhenAny` only notifies on changes of the output value. It only tells you when the final value of the expression has changed. If any intermediate values changed, then the subscriptions will be updated again, but you won't get a new notification on the primary observable if the final value hasn't changed:

```csharp
this.WhenAnyValue(x => x.Foo!.Bar!.Baz)
    .Subscribe(x => Console.WriteLine(x));

this.Foo.Bar.Baz = "Hi!";
// >>> "Hi!"

this.Foo.Bar.Baz = "Hi!";
// nothing happened because the value hasn't changed

this.Foo.Bar = new Bar() { Baz = "Hi!" };
// althought the intermediate value changed, the final value hasn't

this.Foo.Bar = new Bar() { Baz = "Hello!" };
// >>> "Hello!"
```

## Best Practices

## Threading

**Always** set properties in the View Model on the UI thread. The Views should **always** act on the UI thread.

### View to View Model Bindings

**Always** use `OneWayBind` if the property can't be changed from the View:

```csharp
this.WhenActivated(disposables =>
{
    this.OneWayBind(ViewModel, vm => vm.Text, view => view.MyTextBlock.Text)
        .DisposeWith(disposables);
});
```

**Always** use `TwoWayBind` if the property can be changed from the View:

```csharp
this.WhenActivated(disposables =>
{
    this.OneWayBind(ViewModel, vm => vm.IsChecked, view => view.MyCheckBox.IsChecked)
        .DisposeWith(disposables);
});
```

### Disposing

**Always** use `DisposeWith` to dispose any reactive bindings:

```csharp
this.WhenActivated(disposables =>
{
    this.OneWayBind(ViewModel, vm => vm.Text, view => view.MyTextBlock.Text)
        .DisposeWith(disposables);
});
```

**Always** dispose subscriptions of observables:

```csharp
public class FooViewModel : ReactiveObject, IActivatableViewModel
{
    [Reactive] public BarViewModel? Other { get; set; }

    public FooViewModel()
    {
        this.WhenActivated(disposables =>
        {
            // This observable references "Other" which has an unknown lifetime.
            this.WhenAnyValue(vm => vm.Other!.Text)
                .Subscribe(text => { })
                .DisposeWith(disposables);
        });
    }
}

public class BarViewModel : ReactiveObject, IActivatableViewModel
{
    [Reactive] public string Text { get; set; } = string.Empty;

    public BarViewModel()
    {
        this.WhenActivated(disposables =>
        {
            // This observable references the View Model itself, it technically doesn't
            // have to be disposed to be cleaned up as no references to other
            // objects are being created. However, to keep our code uniform and easier
            // to read, you must dispose the subscription, even if it doesn't make a difference.
            this.WhenAnyValue(vm => vm.Text)
                .Subscribe(text => { })
                .DisposeWith(disposable);
        });
    }
}
```

---

[Avalonia]: https://docs.avaloniaui.net/
[ReactiveUI]: https://www.reactiveui.net/
[Dynamic Data]: https://dynamic-data.org/
[WPF]: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/
[WinUI]: https://learn.microsoft.com/en-us/windows/apps/winui/
[Skia]: https://skia.org/
