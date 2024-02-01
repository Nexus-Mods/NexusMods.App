# IFileSystem on Absolute Paths

## Context and Problem Statement

With the switch to a `IFileSystem` abstraction (see [ADR 0004](0004-file-system-abstraction.md)) we need to decide where
the
`IFileSystem` should be used, should they be only in components, with `AbsolutePath` remaining just a set of strings, or
should
`AbsolutePath` always be relative to a given `IFileSystem`?

## Decision Drivers

* Ease of use, we want the API to feel natural
* Avoid foot-guns: make it hard to accidentally use the wrong `IFileSystem` in a given context
* Simplicity: we'd like to avoid having to pass around a `IFileSystem` everywhere or using AbsolutePath/IFilesystem
  pairs

## Considered Options

* Pass a `IFileSystem` to every component that needs it.
    * Has a disadvantage of users accidentally passing a path to a component initialized with the wrong `IFileSystem`
    * Some components only need a `IFileSystem` for a single method
    * Some methods on `AbsolutePath` need a `IFileSystem` to work, these methods then become infectious throughout the
      code
    * Not all methods that use a `AbsolutePath` are on a component (e.g. extension methods). These then need to be
      passed a `IFileSystem` as well, which further infects the code with `IFileSystem` references.
    * Some methods don't even make sense when using a IFileSystem attached to the component. For example
      the `FileExtractor` should extract files from and to anywhere, the filesystems involved are irrelevant, the
      filesystem to be used in a given call to the extractor is completely reliant on the path passed in
* Require that every `AbsolutePath` have an attached `IFileSystem`
    * Paths and `IFileSystem` are now tightly coupled, which makes it harder to use them in isolation, but makes it
      harder to accidentally use the wrong `IFileSystem`
    * Components that create `AbsolutePath` instances need to be passed a `IFileSystem` to use, but this makes sense as
      these are the "constructor" methods for the given paths

## Decision Outcome

Since there are cases where attaching `IFileSystem` to a component *cannot* work semantically, the decision was made to
place the
`IFileSystem` on the `AbsolutePath` itself. This means that every `AbsolutePath` has an attached `IFileSystem` and that
only components that need to
create `AbsolutePath` instances ex nihilo need to be passed a `IFileSystem`. If the behavior of such a component needs
to be configured,
it can be done by configuring the `IFileSystem` used in the DI container.

