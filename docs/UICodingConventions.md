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

Next is the property on the View Model that we want to select. Notice that this an **expression**. `Expression<T>` is different from `Func<T>` in that they return an [expression tree](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/expression-trees/). This topic is very complex and you can read the docs if you're interested but in summary, expressions are limited and some language features like the null propagating operator `?` can't be used. The reason we use expressions for bindings is because the expression tree allows the framework to **know** which property is being referenced at runtime. Instead of seeing this as a lambda that gets executed, think of it as you telling the framework which property you want to bind to, as such you also don't need to care about the value being null because you only mention the property, you don't inspect it's value. This will be explained in more detailed in a later section.

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

Compiling the application, running this example and clicking the button will not change the text. As with XAML bindings, we need to notify the view that a property has changed. However, instead of manually implementing `INotifyPropertyChanged`, we can use the provided `ReactiveObject` class which already implements the interface for us:

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

`ReactiveObject` already implements `INotifyPropertyChanged` and has a `OnPropertyChanged` method. However, ReactiveUI also provides a more powerful `RaiseAndSetIfChanged` extension method that is safer, more efficient and has extra features like notifying subscribers about the property **changing** as well as being **changed**.

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

### Exceptions with ReactiveUI

TODO

## Understanding Dynamic Data

Besides the `INotifyPropertyChanged` that we've looked at before, which is used to be notified when the value of a property changed, there is also [`INotifyCollectionChanged`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.specialized.inotifycollectionchanged). This interface notifies listeners of dynamic changes, such as when an item is added and removed. The associated [`CollectionChanged`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.specialized.inotifycollectionchanged.collectionchanged) event and it's event args type [`NotifyCollectionChangedEventArgs`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.specialized.notifycollectionchangedeventargs) contains information like which action caused the event (item added/moved/removed/replaced), which items are new, which items are old and more.

The interface is part of the `System.Collections.Specialized` namespace and is supported natively by the various collection based controls in Avalonia. Default implementations like [`ObservableCollection<T>`](https://learn.microsoft.com/en-us/dotnet/api/System.Collections.ObjectModel.ObservableCollection-1) or it's read-only counterpart [`ReadOnlyObservableCollection<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.objectmodel.readonlyobservablecollection-1) can be used without ReactiveUI or Dynamic Data.

Dynamic Data is the glue that allows those collections to be used in a reactive environment. Instead of using events, it provides observers and observables.

Let's look at an example that displays a bunch of GUIDs using a `ListBox`. The user can click on an "Add" button to add a new GUID, they can select an item from the list and they can click a "Remove" button to remove the selected item from the list:

```csharp
public class MyViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();

    private readonly SourceList<Guid> _sourceList = new();

    private readonly ReadOnlyObservableCollection<string> _items;
    public ReadOnlyObservableCollection<string> Items => _items;

    [Reactive] public int SelectedIndex { get; set; } = -1;

    public ReactiveCommand<Unit, Unit> AddCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveCommand { get; }

    public MyViewModel()
    {
        AddCommand = ReactiveCommand.Create(() =>
        {
            _sourceList.Edit(list => list.Add(Guid.NewGuid()));
        });

        var canRemove = this
            .WhenAnyValue(vm => vm.SelectedIndex)
            .Select(selectedIndex => selectedIndex >= 0 && selectedIndex < _sourceList.Count);

        RemoveCommand = ReactiveCommand.Create(() =>
        {
            _sourceList.Edit(list => list.RemoveAt(SelectedIndex));
        }, canRemove);

        _sourceList
            .Connect()
            .Transform(guid => guid.ToString())
            .Bind(out _items)
            .Subscribe();
    }
}
```

A few things to note about this View Model. First, you will notice that `WhenActivated` is not called at all and none of the subscriptions are disposed. This is because all observables inside this View Model reference the View Model itself, thus not creating any external references.

Secondly, the only part of this code that comes from Dynamic Data is the `SourceList<T>`. There is also `SourceCache<TObject, TKey>` which you should use when your objects have unique identifiers and you don't care about the position, since you'll likely be sorting them anyways. The `SourceCache` is generally considered to be much more mature and has a wider range of operations, so go for the cache first. This example uses a `ListBox` to display the items and the `ListBox.SelectedIndex` property to remove an item at a specific index, and this works best with a `SourceList<T>`.

Finally, updating a `SourceList` or `SourceCache` is usually done via the `Edit` method. In the case of a `SourceList<T>`, it provides you with an `IExtendedList<T>` argument while a `SourceCache<TObject, TKey>` provides an `ISourceUpdater<TObject, TKey>`. The `Edit` method also does "batching" meaning that removing multiple items from the collection in one edit will result in a change set that mentions the removal of multiple items, instead of `n` single item removals. Only after all edit have been done will the subscriber be notified. You can verify this yourself using the following example:

```csharp
AddCommand = ReactiveCommand.Create(() =>
{
    Console.WriteLine("Before edit");
    _sourceList.Edit(list =>
    {
        Console.WriteLine("Start of edit");
        list.Add(Guid.NewGuid());
        Console.WriteLine("End of edit");
    });
    Console.WriteLine("After edit");
});

_sourceList
    .Connect()
    .Transform(guid => guid.ToString())
    .Bind(out _items)
    .Subscribe(_ => Console.WriteLine("In subscription"));
```

The output is the following:

```
Before edit
Start of edit
End of edit
In subscription
After edit
```

```xml
<StackPanel>
    <Button x:Name="AddButton">Add</Button>
    <Button x:Name="RemoveButton">Remove</Button>
    <ListBox x:Name="MyListBox" SelectionMode="Single">
        <ListBox.DataTemplates>
            <DataTemplate DataType="{x:Type system:String}">
                <TextBlock Text="{CompiledBinding}"/>
            </DataTemplate>
        </ListBox.DataTemplates>
    </ListBox>
</StackPanel>
```

```csharp
this.WhenActivated(disposables =>
{
    this.BindCommand(ViewModel, vm => vm.AddCommand, view => view.AddButton)
        .DisposeWith(disposables);

    this.BindCommand(ViewModel, vm => vm.RemoveCommand, view => view.RemoveButton)
        .DisposeWith(disposables);

    // Bind to the public Items property, never to the SourceCache!
    this.OneWayBind(ViewModel, vm => vm.Items, view => view.MyListBox.ItemsSource)
        .DisposeWith(disposables);
});
```

The View itself has a surprise you might've not expected. While we're using reactive bindings to bind to the `ListBox.ItemsSource` property, the control expects us to provide a `DataTemplate` that is used to actually render the items. In the example, the item type is `string` in which case you can use XAML bindings. If the item type is a View Model, you don't need to use XAML bindings at all. ReactiveUI comes with a built-in feature that allows it to create Views from View Models. If you have a View Model, the framework can look for all registered Views, construct the matching View, and bind the View Model to it.

By default, ReactiveUI uses Splat for dependency injection and using the following code will scan the entire assembly for Views that implement `IViewFor<TViewModel>` and associates them with the corresponding `TViewModel`:

```csharp
public partial class App
{
    public App()
    {
        Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetCallingAssembly());
    }
}
```

To better understand how this works, let's create a `StringView` and `StringViewModel`:

```csharp
public class StringViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();

    public readonly string Text;

    public StringViewModel(string text)
    {
        Text = text;
    }
}
```

```csharp
public partial class StringView : ReactiveUserControl<StringViewModel>
{
    public StringView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Do(PopulateFromViewModel)
                .Subscribe()
                .DisposeWith(disposables);
        });
    }

    private void PopulateFromViewModel(StringViewModel vm)
    {
        MyTextBlock.Text = vm.Text;
    }
}
```

This is overkill to display a single read-only field but it serves to illustrate how View resolution works. Also notice that the code-behind of the View is slightly different from the usual code. If the View Model properties don't change over time, probably because they are read-only, you should set the control properties directly instead of using bindings. This is more efficient than bindings since you only have a subscription on the View Model instead of every property.

The `MyViewModel.Items` collection also has to be updated to use `StringViewModel` instead of `string`:

```csharp
private readonly ReadOnlyObservableCollection<StringViewModel> _items;
public ReadOnlyObservableCollection<StringViewModel> Items => _items;
```

Finally, the View can be simplified and the XAML bindings can be removed:

```xml
<StackPanel>
    <Button x:Name="AddButton">Add</Button>
    <Button x:Name="RemoveButton">Remove</Button>
    <ListBox x:Name="MyListBox" SelectionMode="Single" />
</StackPanel>
```

At runtime, ReactiveUI will find the correct View for the View Model and instantiate it. The `Avalonia.ReactiveUI` package also comes with a `ViewModelViewHost` control:

```xml
<StackPanel>
    <Button x:Name="AddButton">Add</Button>
    <Button x:Name="RemoveButton">Remove</Button>
    <ListBox x:Name="MyListBox" SelectionMode="Single">
        <ListBox.DataTemplates>
            <DataTemplate DataType="{x:Type ui:StringViewModel}">
                <reactive:ViewModelViewHost ViewModel="{CompiledBinding}"/>
            </DataTemplate>
        </ListBox.DataTemplates>
    </ListBox>
</StackPanel>
```

This will behave similar at runtime but we're now using XAML bindings again, which is highly discouraged. The `ViewModelViewHost` control should only be used if you can directly bind to the `ViewModel` property. This is useful if you have a View that can display potentially any View Model, eg if you have container-like Views.

### Nesting View Models in View Models

Once you start using observable collections with View Models you might end up in a scenario where you have a "parent" View Model and multiple "child" View Models that get created by the parent:

```csharp
public class MyViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();

    private readonly SourceCache<ChildViewModel, Guid> _sourceCache = new(child => child.Id);
    private readonly ReadOnlyObservableCollection<ChildViewModel> _children;
    public ReadOnlyObservableCollection<ChildViewModel> Children => _children;

    public readonly ReactiveCommand<Unit, Unit> AddChildCommand;

    public MyViewModel()
    {
        _sourceCache
            .Connect()
            .Bind(out _children)
            .DisposeMany()
            .Subscribe();

        AddChildCommand = ReactiveCommand.Create(() =>
        {
            _sourceCache.Edit(updater =>
            {
                updater.AddOrUpdate(new ChildViewModel());
            });
        });
    }
}

public class ChildViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();

    public Guid Id { get; }
    public string Name { get; }

    public ChildViewModel()
    {
        Id = Guid.NewGuid();
        Name = Id.ToString("D");
    }
}
```

This can be pretty common but requires some design decisions to be made before continuing.

#### Parent reacting to changes in one of the children

We previously learned that ReactiveUI supports expression chaining using `this.WhenAnyValue(x => x.Foo.Bar.Baz)` but this only works if each property in this chain is a single item and not a collection.

For this example, let's assume the View for the `ChildViewModel` contains a `CheckBox` that is bound to the `IsChecked` property:

```csharp
public class ChildViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();

    public Guid Id { get; }
    public string Name { get; }

    [Reactive] public bool IsChecked { get; set; }

    public ChildViewModel()
    {
        Id = Guid.NewGuid();
        Name = Id.ToString("D");
    }
}
```

The parent wants to be notifies when the `IsChecked` property on any of the children changes. This can be easily using Dynamic Data:

```csharp
this.WhenActivated(disposables =>
{
    _sourceCache
        .Connect()
        .WhenValueChanged(child => child.IsChecked)
        .Subscribe(newValue => Console.WriteLine(newValue))
        .DisposeWith(disposables);
});
```

Adding a new item to the source cache will print `False` because that's the initial value. When clicking the checkbox, the console will print `True` and unchecking the checkbox will print `False` again. This works for any amount of children.

The extension method `WhenValueChanged` is part of Dynamic Data and has an optional parameter to change this behavior:

```csharp
this.WhenActivated(disposables =>
{
    _sourceCache
        .Connect()
        .WhenValueChanged(child => child.IsChecked, notifyOnInitialValue: false)
        .Subscribe(newValue => Console.WriteLine(newValue))
        .DisposeWith(disposables);
});
```

You can also replace `WhenValueChanged` with the more powerful version `WhenPropertyChanged`:

```csharp
this.WhenActivated(disposables =>
{
    _sourceCache
        .Connect()
        .WhenPropertyChanged(child => child.IsChecked)
        .Subscribe(propertyValue => Console.WriteLine($"Sender: {propertyValue.Sender.Id} Value: {propertyValue.Value}"))
        .DisposeWith(disposables);
});
```

Instead of only getting the value, you get a tuple that contains the sender and the value. Note that both `WhenValueChanged` and `WhenPropertyChanged` create observables and subscriptions on each of the children, so these should be properly cleaned up using `DisposeWith`.

#### Children sending notifications to the parent

The previous scenario was about the parent reacting to changes in the child, but what if the child wants to send a notification to the parent?

Let's assume the View of the child has a "Remove" `Button` that, when clicked, will remove the child from the list. This requires the child View Model to have a reactive command `RemoveCommand` that is bound to the "Remove" `Button`. When the `RemoveCommand` is being executed, it has to tell the parent to remove the child.

This problem has multiple solutions and I go through you some of them to illustrate the differences.

The first idea would be to have a `RemoveChild` method on the parent and pass the parent to the child when it gets instantiated:

```csharp
public MyViewModel()
{
    AddChildCommand = ReactiveCommand.Create(() =>
    {
        _sourceCache.Edit(updater =>
        {
            // pass "this" to the child
            updater.AddOrUpdate(new ChildViewModel(this));
        });
    });
}

public void RemoveChild(Guid childId)
{
    // with a source cache we only need an ID to remove the child
    _sourceCache.Edit(updater =>
    {
        updater.Remove(childId);
    });
}

public ChildViewModel(MyViewModel parent)
{
    Id = Guid.NewGuid();
    Name = Id.ToString("D");

    RemoveCommand = ReactiveCommand.Create(() =>
    {
        // simply call the method on the parent
        parent.RemoveChild(Id);
    });
}
```

Instead of calling a method, we could also have a reactive command:

```csharp
public MyViewModel()
{
    RemoveChildCommand = ReactiveCommand.Create<Guid>(childId =>
    {
        _sourceCache.Edit(updater =>
        {
            updater.Remove(childId);
        });
    });
}

// pass the parent directly
public ChildViewModel(MyViewModel parent)
{
    Id = Guid.NewGuid();
    Name = Id.ToString("D");

    RemoveCommand = ReactiveCommand.Create(() =>
    {
        // the Execute method returns a "cold" observable, that doesn't do anything until
        // someone subscribes to it
        using var disposable = parent.RemoveChildCommand.Execute(Id).Subscribe();
    });
}

// or just pass the command directly
public ChildViewModel(ReactiveCommand<Guid, Unit> removeChildCommand)
{
    Id = Guid.NewGuid();
    Name = Id.ToString("D");

    RemoveCommand = ReactiveCommand.Create(() =>
    {
        using var disposable = removeChildCommand.Execute(Id).Subscribe();
    });
}
```

You can pass an `IObserver<Guid>` to the child View Model:

```csharp
private readonly Subject<Guid> _removeChildSubject = new();

public MyViewModel()
{
    this.WhenActivated(disposables =>
    {
        _removeChildSubject.Subscribe(childId =>
        {
            _sourceCache.Edit(updater =>
            {
                updater.Remove(childId);
            });
        }).DisposeWith(disposables);
    });
}

public ChildViewModel(IObserver<Guid> removeChildObserver)
{
    Id = Guid.NewGuid();
    Name = Id.ToString("D");

    RemoveCommand = ReactiveCommand.Create(() =>
    {
        removeChildObserver.OnNext(Id);
    });
}
```

Finally, there a solution that doesn't pass **anything** to the child:

```csharp
public ChildViewModel()
{
    Id = Guid.NewGuid();
    Name = Id.ToString("D");

    RemoveCommand = ReactiveCommand.Create(() => Id);
}
```

The child View Model will have a remove command that does nothing because removing the child is responsibility of the parent. The parent can make use of the fact that reactive commands also implement `IObservable<TResult>`, meaning the parent can subscribe to the remove command of the child and do something when the command finished executing:

```csharp
this.WhenActivated(disposables =>
{
    _sourceCache
        .Connect()
        .MergeMany(child => child.RemoveCommand)
        .Subscribe(childId =>
        {
            _sourceCache.Edit(updater =>
            {
                updater.Remove(childId);
            });
        })
        .DisposeWith(disposables);
});
```

The star of this solution is `MergeMany` which is part of Dynamic Data and merges the selected observable on each item. It automatically handles subscriptions for items being added and removed. This code is ideal because it keeps the child stupid and simple.

### Trees

Dynamic Data also supports trees without tree structures. The data internally is flat. As an example, we'll create a `PersonViewModel`:

```csharp
public class PersonViewModel : ReactiveObject, IActivatableView
{
    public ViewModelActivator Activator { get; } = new();

    public Guid Id { get; }
    public string Name { get; }
    public Guid ParentId { get; }

    public ReactiveCommand<Unit, Guid> RemoveCommand { get; }

    public PersonViewModel(Guid id, Guid parentId)
    {
        Id = id;
        ParentId = parentId;
        Name = id.ToString("D");

        RemoveCommand = ReactiveCommand.Create(() => id);
    }
}
```

The important bit is the fact that the object doesn't contain a reference to other objects, it just has an ID that links them together.

```csharp
public class MyViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();

    private readonly SourceCache<PersonViewModel, Guid> _sourceCache = new(x => x.Id);
    private readonly ReadOnlyObservableCollection<NodeViewModel> _nodes;
    public ReadOnlyObservableCollection<NodeViewModel> Nodes => _nodes;

    public MyViewModel()
    {
        _sourceCache
            .Connect()
            .TransformToTree(item => item.ParentId)
            .Transform(node => new NodeViewModel(node))
            .Bind(out _nodes)
            .DisposeMany()
            .Subscribe();

        this.WhenActivated(disposables =>
        {
            _sourceCache
                .Connect()
                .MergeMany(child => child.RemoveCommand)
                .Subscribe(childId =>
                {
                    _sourceCache.Edit(updater =>
                    {
                        updater.Remove(childId);
                    });
                })
                .DisposeWith(disposables);
        });
    }
}
```

The magic method is `TransformToTree` from Dynamic Data. This single method transforms our input into a fully recursive tree using a pivot point we specify. This is going to be the `ParentId` for the example. `TransformToTree` transforms the `IObservable<IChangeSet<PersonViewModel, Guid>>` into an `IObservable<IChangeSet<Node<PersonViewModel, Guid>, Guid>>`. This `Node<TObject, TKey>` type also comes from Dynamic Data and needs to be transformed into a custom View Model for Avalonia:

```csharp
public class NodeViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();

    private readonly ReadOnlyObservableCollection<NodeViewModel> _children;
    public ReadOnlyObservableCollection<NodeViewModel> Children => _children;

    public PersonViewModel Item { get; }

    public NodeViewModel(Node<PersonViewModel, Guid> node)
    {
        Item = node.Item;

        node.Children
            .Connect()
            .Transform(child => new NodeViewModel(child))
            .Bind(out _children)
            .DisposeMany()
            .Subscribe();
    }
}
```

We convert `Node<TObject, TKey>` into a `NodeViewModel` to be able to display them in Avalonia using a `TreeView`:

```xml
<ScrollViewer Background="White">
    <TreeView x:Name="MyTreeView">
        <TreeView.ItemTemplate>
            <TreeDataTemplate DataType="{x:Type ui:NodeViewModel}" ItemsSource="{CompiledBinding Children}">
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="{CompiledBinding Item.Name}" />
                    <Button Command="{CompiledBinding Item.RemoveCommand}">Remove</Button>
                </StackPanel>
            </TreeDataTemplate>
        </TreeView.ItemTemplate>
    </TreeView>
</ScrollViewer>
```

The code-behind only has to bind to the `ItemsSource` property of the `TreeView` and it just works out-of-the-box:

```csharp
this.WhenActivated(disposables =>
{
    this.OneWayBind(ViewModel, vm => vm.Nodes, view => view.MyTreeView.ItemsSource)
        .DisposeWith(disposables);
});
```

## NexusMods.App.UI

This section will be completely about the specifics of the project and the differences to a "normal" Avalonia UI project.

The project [`NexusMods.App.UI`](../src/NexusMods.App.UI) was built with dependency injection in mind. We have a custom View locator that using the DI system called [`InjectedViewLocator`](../src/NexusMods.App.UI/InjectedViewLocator.cs). As mentioned previously, ReactiveUI can construct a View from a View Model. By default, this is done using an assembly scanner that looks for `IViewFor<TViewModel>` implementations and links them to the `TViewModel` type. When we request a View for `TViewModel`, the framework knows that `IViewFor<TViewModel>` exists and tries to construct it using the default constructor.

In this project, the Views are creating using DI, meaning that you have to register the Views and View Model beforehand in the [`Services`](../src/NexusMods.App.UI/Services.cs) file:

```csharp
.AddView<MyView, IMyViewModel>()
.AddViewModel<MyViewModel, IMyViewModel>()
```

For `AddView`, the View has to implement `IViewFor<TViewModel>`. You will get this interface for free by using `ReactiveUserControl<TViewModel>` or `ReactiveWindow<TViewModel>`. The actual View Model being referenced is an interface. The interface has to extend `IViewModelInterface` which is a marker interface:

```csharp
public interface IMyViewModel : IViewModelInterface
{
    public string Name { get; set; }
}
```

The implementation of this interface `MyViewModel` would look like this:

```csharp
public class MyViewModel : AViewModel<IMyViewModel>, IMyViewModel
{
    [Reactive]
    public string Name { get; set; } = string.Empty;
}
```

The abstract class `AViewModel<TViewModel>` and the `IViewModelInterface` are custom made to be used in our DI system. `AViewModel<TViewModel>` inherits from `ReactiveObject` and implements `IActivatableViewModel`, so you can freely use `this.WhenActivated` inside the constructor.

The benefit of using an interface for the `TViewModel` type parameter is being able to have different implementations. You'll usually find two implementations in the project, one for design time and another for runtime:

```csharp
public class MyViewModel : AViewModel<IMyViewModel>, IMyViewModel
{
    [Reactive]
    public string Name { get; set; } = string.Empty;
}

public class MyDesignViewModel : AViewModel<IMyViewModel>, IMyViewModel
{
    public string Name { get; set; } = "This is some design default value";
}
```

We can use the design View Model by setting the design data context in the View:

```xml
<Design.DataContext>
    <local:MyDesignViewModel />
</Design.DataContext>
```

Using a design View Model is great if the View Model contains only data and next to no logic or commands. You should be mindful of not replicating any logic of the normal View Model inside the design View Model, as it often results in duplicate, messy and/or less maintainable code. The design View Model can also inherit from the normal View Model if that makes it easier.

In summary, you'll need to create four (+1 code-behind) files in most cases:

- `IMyViewModel`: extends `IViewModelInterface`
- `MyView`: inherits from `ReactiveUserControl<IMyViewModel>`
- `MyViewModel`: inherits from `AViewModel<IMyViewModel>` and implements `IMyViewModel`
- `MyDesignViewModel`: inherits from `AViewModel<IMyViewModel>` and implements `IMyViewModel`

These files should be grouped together in a folder that isn't a namespace provider. You should also link the View Model implementations to the interface, similar to how the code-behind file is linked to the View:

```
|
|- IMyViewModel.cs
|--- MyViewModel.cs
|--- MyDesignViewModel.cs
|- MyView.axaml
|--- MyView.axaml.cs
```

This is, understandably, quite a lot of boilerplate just to create a new View. You can use the `NexusMods MVVM` file templates in Rider to create all files instantly.

## Best Practices

### Threading

**Always** set properties in the View Model on the UI thread. The Views should **always** act on the UI thread.

### View Model Properties

**Always** use `ObservableAsPropertyHelper<T>` to expose the latest values from an `IObservable<T>` that is async or runs on the task pool scheduler:

```csharp
public class BadExampleViewModel
{
    [Reactive]
    public string Text { get; set; } = string.Empty;

    public BadExampleViewModel()
    {
        this.WhenActivated(disposables =>
        {
            // don't use subscribe to set the property
            Observable
                .Return("Hi!")
                .Subscribe(text => Text = text)
                .DiposeWith(disposables);
        });
    }
}

public class GoodExampleViewModel
{
    private readonly ObservableAsPropertyHelper<string> _text;
    public string Text => _text;

    public GoodExampleViewModel()
    {
        _text = Observable
            .Return("Hi!")
            // set the scheduler to be on the UI thread instead of calling OnUI
            .ToProperty(this, vm => vm.Text, scheduler: RxApp.MainThreadScheduler);

        this.WhenActivated(disposables =>
        {
            Disposable.Create(() => _text.Dispose()).DisposeWith(disposables);
        });
    }
}
```

**Always** use `BindTo` and a reactive property to expose the latest values from an `IObservable<T>` that returns immediately and isn't doing anything async:

```csharp
public class GoodExampleViewModel
{
    [Reactive] public string Text { get; private set; }

    public GoodExampleViewModel()
    {
        this.WhenActivated(disposables =>
        {
            _text = Observable
                .Return("Hi!")
                .BindTo(this, vm => vm.Text)
                .DiposeWith(disposables);
        });
    }
}
```

### View to View Model Bindings

**Always** populate the View directly with values from the View Model **if** the properties don't change over time:

```csharp
public MyView()
{
    InitializeComponent();

    this.WhenActivated(disposables =>
    {
        this.WhenAnyValue(view => view.ViewModel)
            .WhereNotNull()
            .Do(PopulateFromViewModel)
            .Subscribe()
            .DisposeWith(disposables);
    });
}

private void PopulateFromViewModel(MyViewModel vm)
{
    MyTextBlock.Text = vm.Text;
}
```

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

### Dynamic Data

**Never** expose the `SourceList<T>` or `SourceCache<TObject, TKey>` field to the View. These fields should **always** be marked as `private readonly` and the only public properties should either be an `IObservable<IChangeSet<T>>` that is the result from calling `.Connect` or `ObservableCollection<T>` or `ReadOnlyObservableCollection<T>`.

**Always** try using `SourceCache<TObject, TKey>` first. If the object type doesn't have an Id, you can always just add one using `Guid`. `SourceList<T>` has less APIs and features, like the ones shown in the following examples.

For a parent reacting to changes in any of the children, use `WhenValueChanged` when you only need the value and `WhenPropertyChanged` when you also need the sender ([example](#parent-reacting-to-changes-in-one-of-the-children)):

```csharp
public class ChildViewModel
{
    // child has a reactive property that the parent wants to observe changes on:
    [Reactive] public bool IsChecked { get; set; }
}

public ParentViewModel()
{
    this.WhenActivated(disposables =>
    {
        _sourceCache
            .Connect()
            // WhenValueChanged and WhenPropertyChanged return a single IObservable<T> for all items
            // they automatically handle new and removed items
            .WhenValueChanged(child => child.IsChecked)
            .Subscribe(newValue => Console.WriteLine(newValue))
            .DisposeWith(disposables);
    });
}
```

Instead of passing a reference of the parent to the child, keep the child simple and stupid and have the parent subscribe to an observable of all children using `MergeMany` ([example](#children-sending-notifications-to-the-parent)):

```csharp
public ChildViewModel()
{
    RemoveCommand = ReactiveCommand.Create(() => Id);
}

public ParentViewModel()
{
    this.WhenActivated(disposables =>
    {
        _sourceCache
            .Connect()
            // ReactiveCommand<TParam, TResult> implements IObservable<TResult>
            .MergeMany(child => child.RemoveCommand)
            .Subscribe(childId =>
            {
                _sourceCache.Edit(updater =>
                {
                    updater.Remove(childId);
                });
            })
            .DisposeWith(disposables);
    });
}
```

---

[Avalonia]: https://docs.avaloniaui.net/
[ReactiveUI]: https://www.reactiveui.net/
[Dynamic Data]: https://dynamic-data.org/
[WPF]: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/
[WinUI]: https://learn.microsoft.com/en-us/windows/apps/winui/
[Skia]: https://skia.org/
