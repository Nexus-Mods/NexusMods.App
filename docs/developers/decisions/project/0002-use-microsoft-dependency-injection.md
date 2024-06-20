# Use Microsoft Dependency Injection

## Context and Problem Statement

In order to build the project with a modular and extensible structure we need a good system
for swapping out components and stubbing parts of the system for testing.

## Decision Drivers

We want a methodology well understood and respected by the platform and runtime we are using
in this case we should probably default to the most commonly used systems only diverting for established
platform practices when we need a feature not supported by these platforms.

## Considered Options

* Microsoft Dependency Injection system
    * Most common option on the .NET platform
* Splat
    * Used by Avalonia, can interop with other DI containers
* Roll-our-own

## Decision Outcome

The Microsoft .NET DI system is extensively used in the .NET ecosystem and is the common choice for almost
all .NET applications. Lots of tutorials exist for this library, it's the simplest approach.

### Consequences

* Good
    * Lots of examples and tutorials online
    * Lots of support by testing systems, and other libraries
* Bad
    * The DI system is immutable, adding new components to the application will require a restart of the application
