!!! tip "Quick Primer to Dependency Injection"

The Nexus Mods App uses Dependency Injection (DI) for creating classes.

Dependency Injection is like having a smart assistant who prepares all the tools
(interfaces, a.k.a. abstract classes, traits) for a class you are trying to create.

This reduces a lot of boilerplate, helps with maintenance and makes our code cleaner.

## A Quick Primer

!!! nexus "In the Nexus Mods App, we mostly use 'Constructor Injection'"

When you create a new class, you specify what 'services' it needs right in the constructor, via interfaces.

To instantiate the class, you then call `serviceProvider.GetRequiredService<MyClass>()`, where
serviceProvider is an `IServiceProvider`.

The DI system then automatically provides these 'services' when it creates an instance of your class.

### Example: Creating a Class With and Without DI

Let's look at a simple example of how a service is resolved with and without DI. This can be shown in two tabs for clarity.

=== "With Dependency Injection"

    ```csharp
    public class MyClass
    {
        private readonly IFoo _foo;
        private readonly IBar _bar;
        private readonly IBaz _baz;

        public MyClass(IFoo foo, IBar bar, IBaz baz)
        {
            _foo = foo; // Foo is provided by DI
            _bar = bar; // Bar is provided by DI
            _baz = baz; // Baz is provided by DI
        }
    }

    // Create Class
    var myGame = serviceProvider.GetRequiredService<MyClass>();
    ```

    When we ask the DI system for an instance of `IFoo`, `IBar`, and `IBaz` by calling `serviceProvider.GetRequiredService<MyClass>()`,
    the DI system automatically provides `IFoo`, `IBar` and `IBaz` for the class.

=== "Without Dependency Injection"

    ```csharp
    public class MyClass
    {
        private readonly IFoo _foo;
        private readonly IBar _bar;
        private readonly IBaz _baz;

        public MyClass(IFoo foo, IBar bar, IBaz baz)
        {
            _foo = foo; // Foo is provided by user
            _bar = bar; // Bar is provided by user
            _baz = baz; // Baz is provided by user
        }
    }

    // Create Class
    var myGame = new MyClass(
        new Foo( new SomethingFooNeeds(), new SomethingOtherFooNeeds() ),
        new Bar(),
        new Baz( new SomethingBazNeeds() ),
    );
    ```

    Without DI, we have to manually create `IFoo`, `IBar` and `IBaz` that are needed by `MyClass`.
    This can get very messy and inflexible over the course of the long run.

### How Does it Know

!!! question "How does the DI 'magically' know what to insert to the constructor?"

Each of our projects has a `Services.cs` file which contains a method called `Add<ServiceName>`. For example, `AddFileExtractors`.

When we start the App or Tests, we add services to an `IServiceCollection` (see [AddApp][add-app]), to add our different 'services'.

??? "Example `Add<ServiceName>` (Click to Expand)"

    ```csharp
    /// <summary>
    /// Adds file extraction related services to the provided DI container.
    /// </summary>
    /// <param name="coll">Service collection to register.</param>
    /// <param name="settings">Settings for the extractor.</param>
    /// <returns>Service collection passed as parameter.</returns>
    public static IServiceCollection AddFileExtractors(this IServiceCollection coll, IFileExtractorSettings? settings = null)
    {
        if (settings == null)
            coll.AddSingleton<IFileExtractorSettings, FileExtractorSettings>();
        else
            coll.AddSingleton(settings);

        coll.AddSingleton<IFileExtractor, FileExtractor>();
        coll.AddSingleton<IExtractor, SevenZipExtractor>();
        coll.TryAddSingleton<TemporaryFileManager, TemporaryFileManagerEx>();
        return coll;
    }
    ```

Example:

1. **Register 'Foo' as 'Foo'**:
   ```csharp
   services.AddSingleton<Foo>();
   ```

2. **Register 'Foo' as 'IFoo'**:
   ```csharp
   services.AddSingleton<IFoo, Foo>();
   ```

3. **Register Foo as 'IFoo' and 'Foo'**:
   ```csharp
   services.AddAllSingleton<IFoo, Foo>();
   ```

!!! note "Although we mostly use `Singleton` in the Nexus Mods App, services can have different `lifetimes`"

1. **Singleton**: Only one instance, instance is reused for every requested parameter. (like `static` field)
   ```csharp
   // Only one instance of Foo is created and used everywhere
   services.AddSingleton<IFoo, Foo>();
   ```
2. **Scoped**: Created and reused in each 'scope' (`CreateScope()`). Scopes can be used to e.g. handle a web request.
   ```csharp
   // A new single instance of MyService is created for each scope
   services.AddScoped<IFoo, Foo>();
   ```
3. **Transient**: A new instance is created every time.
   ```csharp
   // A new instance of MyService is created each time it's requested
   services.AddTransient<IFoo, Foo>();
   ```

### Adding Dependency Injection to your New Project

!!! tip "If you are creating a new project, you'll need to manually add dependency injection."

To do this, add a `Services.cs` file to your project, which should look something like this:

```csharp
public static class Services
{
    public static IServiceCollection AddYourThing(this IServiceCollection services)
    {
        // example
        // services.AddAllSingleton<ITypeFinder, TypeFinder>();
        return services;
    }
}
```

And add the NuGet package `Microsoft.Extensions.DependencyInjection.Abstractions` to your project. You can usually do it
by right clicking and hitting `Manage NuGet Packages` in your IDE of choice.

## Tips and Tricks

!!! info "Obtaining Multiple Instances of a Registration"

It's possible to retrieve all implementations registered for a particular interface.

This is useful when you have multiple classes that fulfill the same role but in different ways.

For example, to get all registered implementations of `IGame` (all games):

```csharp
// Returns all registered IGame implementations
var games = services.GetRequiredService<IEnumerable<IGame>>();
```

[add-app]: https://github.com/Nexus-Mods/NexusMods.App/blob/71ed7f186c6a5fe0dd0e45e2cf24c7a624c1bed4/src/NexusMods.App/Services.cs#L51
