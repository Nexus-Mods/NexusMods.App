## Filesystem Organization

There are two types of projects: projects that provide code and projects that test code. All projects that provide code should go into the `src` directory and all projects that test code should go into the `tests` directory.

We use `Directory.Build.props` files in both the top level directory and the `src` and `tests` subdirectories to provide common configurations for all projects in those directories. Having subdirectories in `src` and `tests` that aren't project directories can sometimes mess with that setup and introduces unnecessary differences between project files when importing properties from files like `NuGet.Build.props`.

- ❌ `src/Cool Stuff/NexusMods.Foo/NexusMods.Foo.csproj`
- ✅ `src/NexusMods.Foo/NexusMods.Foo.csproj`

Solutions allow you to create "Solution Folders" which are virtual directories that you can use to group projects together. Use those virtual directories instead of filesystem directories for grouping projects.

## Naming

### Namespace

The namespace should be the same as the project name:

- ❌ `src/NexusMods.Foo/NexusMods.Foo.csproj` with namespace `NexusMods.Bar`
- ✅ `src/NexusMods.Foo/NexusMods.Foo.csproj` with namespace `NexusMods.Foo`

### Prefix

All projects should start with the same prefix `NexusMods.`. For consistency, any other prefixes aren't allowed:

- ❌ `NexusMods.App.Foo`
- ❌ `NexusMods.App.Bar`
- ❌ `NexusMods.Abstractions.Foo`
- ❌ `NexusMods.Abstractions.Bar`
- ✅ `NexusMods.Foo`
- ✅ `NexusMods.Bar`
- ✅ `NexusMods.App`

### Suffix

Projects can have a suffix describing the type of code that's in the project:

- ❌ `.Abstractions`
- ❌ `.Core`
- ❌ `.Lib`
- ❌ `.Utils`/`.Utilities`
- ✅ `.Tests`: for unit test projects
- ✅ `.UI`: for projects containing `axaml`/`xaml` files

### Tests

Unit test projects should only test one project and can thus be named after the original project and suffixed with `.Tests`. Example:

- Project providing code: `NexusMods.Foo`
- Project testing the code: `NexusMods.Foo.Tests`
