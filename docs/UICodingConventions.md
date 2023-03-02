# UI Coding Conventions and Guidelines

## Introduction
This document serves as a style guide and documentation resource for the UI portions of the NexusMods App. C# UI design
is hardly a solved problem, and there are many different ways to do things. This document will serve as a guide to help in the
standardization of UI design and development.

## Tech Stack
The UI is built on Avalonia, which is a cross-platform UI framework. Interfaces are defined in XAML, and we use ReactiveUI and
the MVVM pattern to bind the UI to the underlying data model.

* XAML -A XML-based markup language for describing the UI. XAML is used to define the UI elements, their properties, and their styling.
* ReactiveUI - A framework for building reactive and functional applications on .NET. It's a extension of the concepts found in [Reactive Extensions (Rx)](https://reactivex.io/) and [Functional Reactive Programming (FRP)](https://en.wikipedia.org/wiki/Functional_reactive_programming).
* Reactive Extensions (Rx) - A library for composing asynchronous and event-based programs using observable sequences and LINQ-style query operators. In normal Linq, items are pulled "through" the Linq chain, in Rx they are "Pushed" through the chain, events vs enumeration.
* Dynamic Data - A library for creating observable collections from observable sources. It provides a set of operators for transforming, filtering, aggregating and joining observable collections. This can take a stream of "updates" and turn it into a cached collection of the aggregate state.

## MVVM
The MVVM pattern is a UI design pattern that consists of three major parts

### The Model
The model components are the underlying business logic. These components should be devoid of UI logic, and be separated from UI components. They should be reusable by CLI applications, or other UI frameworks. In out application the Model is any library not in the `NexusMods.App.UI` project

### The View
A UI component that displays data. The View should take care of its own styling, and should not be concerned with the underlying data model. In our application, the View is defined in XAML.

### The ViewModel
A ViewModel is a class that exposes data from the model to the view. It should not contain any UI logic, and should be reusable by other views. In our application, the ViewModel is defined in C#.
The View model exposes data via `INotifyPropertyChanged` interfaces and by Reactive `IObservable` properties. ReactiveUI.Fody is used to auto generate `INotifyPropertyChanged` events whenever a property on the view model changes.

## Dependency Injection and Design Time
The IDEs in use for C# can run parts of the app to present a real time preview of the UI. This is called Design Time. The IDEs can also run the app in a debug mode, which is called Debug Time. In Debug Time, the app is run in a normal way, and the UI is updated in real time. In Design Time, the app is run in a special mode where the UI is updated in real time, but the app is not actually run. This is useful for designing the UI without having to run the app.
However this means that for views to work properly they need a ViewModel that behaves somewhat like a real ViewModel. This allows the View to be tested in the IDE. Since we inject our ViewModels at runtime, this means we often need two versions of each ViewModel one for runtime and one for deign time.

At runtime, whenever the app sees a usage of the `ViewModelViewHost` control, it will look at the control's `ViewModel` property and use the DI system to find the appropriate View for displaying the ViewModel's data.

### DI coding conventions
For each view/custom control:
  * Create a View named `FooView.xaml`
  * Create a ViewModel Interface with a formal definition of the ViewModel named `IFooViewModel.cs`
  * Create a design-time implementation of the ViewModel named `FooDesignViewModel.cs`
  * Create a runtime implementation of the ViewModel named `FooViewModel.cs` that implements the `IFooViewModel` interface and inherits from `AViewModel<IFooViewModel>`
  * Register the view in the DI system and mark it as a view for the view model `services.AddView<FooView, IFooViewModel>();`
  * Use the design time ViewModel in the View XAML via `<Design.DataContext>...</Design.DataContext>`
  * Register the runtime ViewModel in the DI system `services.AddViewModel<FooViewModel>();`

## Styling vs new controls
When designing a component, consider if an existing component (from the Avalonia library) can be re-styled, instead of implementing a new control. Become familiar with the Avalonia styling system, as it is very powerful and can be used to create visually distinct controls without having to implement them from scratch.

## Naming Conventions

* Views - Should be suffixed with `View` and should be in PascalCase. For example `FooView.xaml`
* Design Time ViewModels - Should be suffixed with `DesignViewModel` and should be in PascalCase. For example `FooDesignViewModel.cs`
* ViewModels - Should be suffixed with `ViewModel` and should be in PascalCase. For example `FooViewModel.cs`
* ViewModel Interfaces - Should be prefixed with `I` and should be in PascalCase. For example `IFooViewModel.cs`
* All the Views/VewModels/ViewModel Interfaces should be in the same folder as the View, and in a folder named for the control. If multiple views exist for the same ViewModel they can be added to the same folder.

## XAML Coding Conventions

* Name components in PascalCase, suffixed with the type of component. For example `FooButton`, `FooLabel`, `FooTextBox`

## Wiring Conventions

* Use ReactiveUI.Fody to auto generate `INotifyPropertyChanged` events whenever a property on the view model changes.
* The app uses `XamlNameReferenceGenerator` to generate fields on views for any controls with a `x:Name=` attribute, but this requires that the view calls the generated `InitializeComponent()` method. So delete the auto-generated `InitializeComponent()` method when a new view is created, but keep the call in the constructor. The fields in the view can then be used to access the controls in the view after `InitializeComponent()` is called.
* Use `IObservable<T>` for properties that are a stream of changes.
* Use `DynamicData` to flatten and transform `IObservable<T>` properties into collections of data.
* Avoid usage of `{Binding ...}` in favor of RX and DynamicData operators. This binding syntax requires the creation of a lot of transformers and converter classes which can be a single `.Select` call in Rx.
* Be careful of making changes to UI components from a non-UI thread. This can cause the UI to error out and become unusable.
  * For IObservable properties, the `.OnUI()` operator can be used to ensure that the rest of the chain is run on the UI thread.
  * For `.Bind()` calls there's a helper `.BindUI()` method that will ensure that the binding is run on the UI thread.
* Perform heavy compute logic off of the main UI thread. We currently don't have a way to switch to a non-UI thread, but any call to `Task.Run()` will run on a non-UI thread.
* Try to drive as much behavior from `IDataStore` as possible. This will allow the UI to be updated in real time when the data changes even when the change comes from another process. If pulling data from IDataStore becomes too slow, we will improve the performance (and caching) of the store to compensate.
* Keep all UI code in the `NexusMods.App.UI` project. Having UI code in game specific projects is an anti-pattern and should be avoided. If a game requires specialized UI logic, that logic should be abstracted and added as a generalized feature to the main UI project.
