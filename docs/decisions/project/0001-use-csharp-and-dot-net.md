# Use C# and .NET for project development

## Context and Problem Statement

As part of a rewrite one of the first decisions is to identify an acceptable platform for development,
this should be done at the start of the project to reduce codebase churn.

## Decision Drivers

The following were feature sets considered during the course of this investigation:

* Userbase - approximate relative size of user bases. Looking for orders of magnitude here. So “Large” means perhaps one
  in ten developers have experience with the language, where “Small” means that one in ten developers have heard of the
  language.
* Cross-platform
    * Yes - language runs on all major operating systems
    * Difficult - language runs on all major operating systems but may require significant rewrites on other platforms
    * No - language runs on only one platform
* Multi-threaded
    * Fully - the language allows multiple threads to run concurrently with full shared memory
    * Isolated - multiple threads can run in separate processes/VMs
    * GIL - language has a global interpreter lock of some sort
* Performance
    * Near-native - most optimized code is within 2x slower than C
    * Fast-enough - most code that is not in a tight loop will be un-noticeable from faster languages
    * Slow - anything less than 10% the speed of C
* Intrinsics
    * Yes - Modern processors have quite a number of SHA and Vector related intrinsics such as AVX, SSE, etc. These come
      in handy for operations like compression, hashing and 3d math. This language can access these intrinsics
    * No - The language does not expose CPU level special operators to the language
* GUI support
    * Modern - language has one or more well maintained GUI toolkits that have seen at least 4-5 years of success
      commercially
    * Limited - GUI options that exist are either new, outdated, or unproven
* Cross platform GUI
    * Yes - GUIs that exist for the language are available on non Windows platforms
    * No - GUIs that exist are on a per-platform basis
* Mod community use
    * High - Mod managers, large projects and/or tools are developed in this language
    * Medium - There are a some, but it’s not wide spread
    * Low - it’s hard to think of a program that uses this library
* Game format library support
    * High - if it exists there’s probably some way to access the files with this language
    * Medium - you may have to hunt for the tools but the common ones may exit
    * Low - we’ll have to write everything from scratch
* Alternative languages
    * Yes - there are alternative languages/syntaxes for this runtime
    * No - there is one language for this runtime
* C interop
    * FFI (Foreign Function Interface) - you can declare external C or OS routines inside the application, and call them
      without writing any C code
    * Module - you have to write a C module to interface with C
* Self Contained Runtime
    * Yes - you can publish a single file with all your app code in it, and users do not need to download an external
      library to get your code to run
    * No - you need to install an external runtime
* Batteries Included
    * Yes - there is one main way of developing a specific software pattern maintained by the same group that maintains
      the language
    * No - you will need to mix and match libraries to build your application
* Statically Typed
    * Runtime - language has runtime static typing
    * Compile-time - language has compile-time or optional typing only
* Runtime Reflection
    * Yes - language can self-introspect and get the argument types and member types at runtime
    * No - language has limited or no support for reflection
* Runtime Compilation
    * Yes - Language can generate and execute code at runtime either by parsing strings or invoking a builtin JIT and/or
      compiler
    * No - One the app is built cannot self-extend itself, except via a DLL or other pre-compiled binary

## Considered Options

The considered platforms and options are listed across the top of this table, the various evaluation points are listed
in the rows. A checkmark is placed in a box where the evaluation result
is considered "good" for the given platform.

|                            | Electron / Typescript      | C++            | Python       | Rust        | .NET / C#     | Ruby  | JVM / Java / Kotlin / etc.     | Go          |
|----------------------------|----------------------------|----------------|--------------|-------------|---------------|-------|--------------------------------|-------------|
| Userbase                   | ✓ Large                    | ✓ Large        | ✓ Large      | Small       | ✓ Large       | Small | ✓ Large                        | Small       |
| Cross-Platform             | ✓ Yes                      | Difficult      | ✓ Yes        | ✓ Yes       | ✓ Yes         | ✓ Yes | ✓ Yes                          | ✓ Yes       |
| Multi-threaded             | Isolated (message-passing) | ✓ Yes          | GIL/Isolated | ✓Yes        | ✓ Yes         | GIL   | ✓ Yes                          | ✓ Yes       |
| Performance                | Fast-enough                | ✓ Near-Native  | Slow         | Near-Native | ✓ Near-Native | Slow  | Near-Native                    | Fast-Enough |
| Intrinsics                 | No                         | ✓ Yes          | No           | ✓ Yes       | ✓ Yes         | No    | No                             | Yes         |
| GUI Support                | ✓ Modern                   | ✓ Modern       | ✓ Modern     | Limited     | ✓ Modern      | No    | ✓ Yes                          | No          |
| Cross Platform GUI         | ✓ Yes                      | ✓ Yes          | ✓ Yes        | ✓ Yes       | ✓ Yes         | No    | ✓ Yes                          | No          |
| Mod Community Use          | ✓ Medium                   | ✓ High         | Medium       | Medium      | ✓ High        | Small | Small                          | Small       |
| Game Format Libary Support | ✓ Medium                   | ✓ High         | Medium       | Medium      | ✓ High        | Small | Small                          | Small       |
| Alternative Languages      | ✓ Yes                      | No             | No           | No          | ✓ Yes         | No    | ✓ Yes                          | No          |
| C Interop                  | ✓ FFI                      | ✓ FFI (Native  | ✓ FFI        | ✓ FFI       | ✓ FFI         | ✓ FFI | ✓ FFI                          | ✓ FFI       |
| Self-Contained Runtime     | ✓ Yes                      | ✓ Yes          | ✓ Yes        | ✓ Yes       | ✓ Yes         | No    | ✓ Yes                          | ✓ Yes       |
| Batteries Included         | No                         | No             | ✓ Yes        | No          | ✓ Yes         | No    | ✓ Yes                          | No          |
| Statically Compiled        | Compile Time               | ✓ Runtime      | Compile Time | ✓ Runtime   | ✓ Runtime     | No    | ✓ Runtime (minus type erasure) | ✓ Runtime   |
| Runtime Reflection         | ✓ Yes                      | No (RTTI Only) | ✓ Yes        | No          | ✓ Yes         | ✓ Yes | ✓ Yes                          | No          |
| Runtime Compilation        | ✓ Yes                      | No             | ✓ Yes        | No          | ✓ Yes         | ✓ Yes | ✓ Yes                          | No          |

## Decision Outcome

Chosen option: .NET and C#
The combination of .NET and C# provides the best combination of features, reach and developer ease, without
requiring us to reach for secondary languages, runtimes or build systems.

