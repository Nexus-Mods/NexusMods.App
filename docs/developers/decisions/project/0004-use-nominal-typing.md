# Use Nominal Typing

## Context and Problem Statement

Many parts of our application involve passing small value types that carry context with them that is lost when passing
them via their base types.

For example:

* A file path may be a `string` but it's really a `file path`
* A ModId may be a `int` internally, but should not be added, subtracted, or divided
* In our logging and UI contexts we often want to know what a given string, integer or other such value represents to
  better provide feedback to the user.
* Code can be written in a more terse manner when not prefixed by type specific names, in addition the context can
  become a bit more clear in a fluent context
    * `root.Join(path)` is clearer than `Path.Join(root, path)` or `join(root, path)`

## Decision Drivers

There are several things to consider when wrapping base types, but thankfully most of the drawbacks
of this approach do not apply in the context of .NET. Thanks to structs, wrapper types are mostly allocation free
(existing only on the stack). In addition, thanks
to [Extension Methods](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods)
wrapper and nominal types can be extended by external libraries, and plugins. Since C# also supports
operator overloading it's rather trivial to convert to and from primitive types.

## Considered Options

* Nominal Typing
    * Wrapping primitives in distinct types so that the type checker will not allow a `ModId` to be treated like a
      number or a path like a string
* Using bare (structure) types
    * Let paths remain paths, ModIds remain ints, etc.

## Decision Outcome

Chosen option: Nominal typing. The more context and information we can provide to developers via the typing system, the
better. A `path` will have methods that work with paths and will
not require users to hunt down the libraries they need to include to work with that type. Same with `ModId` and every
other logically distinct type.
