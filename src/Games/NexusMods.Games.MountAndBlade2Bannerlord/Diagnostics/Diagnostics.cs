using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Generators.Diagnostics;
namespace NexusMods.Games.MountAndBlade2Bannerlord.Diagnostics;

internal static partial class Diagnostics
{
    private const string Source = "NexusMods.Games.MountAndBlade2Bannerlord";

    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate MissingDependencyTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 0))
        .WithTitle("Missing Dependency")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("'{ModName}' requires mod with ID '{DependencyId}' which is not installed")
        .WithDetails("""
The mod `{ModName}` requires a mod with the ID `{DependencyId}` to function, but `{DependencyId}` is not installed.

### How to Resolve
1. Download and install mod `{DependencyId}`
2. Enable mod `{DependencyId}`

### Technical Details
Looking at `{ModName}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Current mod is `{ModName}` with id `{ModId}` -->
    <Id value="{ModId}"/>
    <Name value="{ModName}"/>
    <DependedModules>
        <!-- ❌ This dependency `{DependencyId}` is not installed -->
        <DependedModule Id="{DependencyId}" />
    </DependedModules>
</Module>
```

The issue can arise in these scenarios:

1. **Missing Installation**: The dependency was never installed
2. **Disabled Mod**: The dependency exists but isn't enabled in the loadout
3. **Incorrect Mod ID**: The dependency ID might be misspelled [if you are the mod author]
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModId")
            .AddValue<string>("ModName")
            .AddValue<string>("DependencyId")
        )
        .Finish();

    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate MissingVersionRangeTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 1))
        .WithTitle("Missing Dependency")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("'{ModName}' requires '{DependencyId}' version '{VersionRange}' which is not installed")
        .WithDetails("""
The mod `{ModName}` mod requires a mod with the ID `{DependencyId}` within the range `{VersionRange}`, but no compatible version is installed.

### How to Resolve
1. Download a version of mod `{DependencyId}` within the range `{VersionRange}`
2. Install mod `{DependencyId}` into your loadout
3. Enable mod `{DependencyId}`

### Technical Details
Looking at `{ModName}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Current mod is `{ModName}` with id `{ModId}` -->
    <Id value="{ModId}"/>
    <Name value="{ModName}"/>
    <DependedModuleMetadatas>
        <!-- ❌ This dependency `{DependencyId}` (version range `{VersionRange}`) is not installed -->
        <DependedModuleMetadata id="{DependencyId}" version="{VersionRange}" />
    </DependedModuleMetadatas>
</Module>
```

Usually this is because mod with `{DependencyId}` is not installed at all.

If you cannot find a compatible version:

1. Try a newer version of mod `{ModName}` that might support different versions
2. Contact the mod author for guidance
3. Search for archived versions of the mod
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModId")
            .AddValue<string>("ModName")
            .AddValue<string>("DependencyId")
            .AddValue<string>("VersionRange")
        )
        .Finish();

    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate ModVersionRangeTooLowTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 2))
        .WithTitle("Outdated Dependency")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{ModName}' requires '{DependencyId}' version '{VersionRange}' but found older version '{InstalledVersion}'")
        .WithDetails("""
The mod `{ModName}` mod requires a mod with the ID `{DependencyId}` within the version range `{VersionRange}`, but version `{InstalledVersion}` is installed which is too old.

### How to Resolve
1. Update mod `{DependencyId}` from version `{InstalledVersion}` to a version within the range `{VersionRange}`
2. Enable the updated version of mod `{DependencyId}`

### Technical Details
Looking at `{ModName}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Current mod is `{ModName}` with id `{ModId}` -->
    <Id value="{ModId}"/>
    <Name value="{ModName}"/>
    <DependedModuleMetadatas>
        <!-- ❌ `{DependencyId}` version `{InstalledVersion}` is below minimum required range `{VersionRange}` -->
        <DependedModuleMetadata id="{DependencyId}" version="{VersionRange}" />
    </DependedModuleMetadatas>
</Module>
```

Common scenarios:

1. An older version was previously installed
2. Multiple versions are installed and the wrong one is active
3. The mod was installed from an outdated source
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModId")
            .AddValue<string>("ModName")
            .AddValue<string>("DependencyId")
            .AddValue<string>("VersionRange")
            .AddValue<string>("InstalledVersion")
        )
        .Finish();

    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate ModVersionRangeTooHighTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 3))
        .WithTitle("Dependency too new")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{ModName}' requires '{DependencyId}' version '{VersionRange}' but found newer version '{InstalledVersion}'")
        .WithDetails("""
The mod `{ModName}` mod requires a mod with ID `{DependencyId}` within the version range `{VersionRange}`, but version `{InstalledVersion}` is installed which is too new.

### How to Resolve
1. Replace mod `{DependencyId}` version `{InstalledVersion}` with a version within the range `{VersionRange}`
2. Enable the mod you just replaced

### Technical Details
Looking at `{ModName}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Current mod is `{ModName}` with id `{ModId}` -->
    <Id value="{ModId}"/>
    <Name value="{ModName}"/>
    <DependedModuleMetadatas>
        <!-- ❌ `{DependencyId}` version `{InstalledVersion}` exceeds maximum allowed range `{VersionRange}` -->
        <DependedModuleMetadata id="{DependencyId}" version="{VersionRange}" />
    </DependedModuleMetadatas>
</Module>
```

Common scenarios:

1. A dependency was automatically updated to a newer incompatible version
2. Multiple versions are installed and the wrong one is active
3. The mod hasn't been updated to support newer dependency versions

If you cannot find an older version:

1. Check if a newer version of `{ModName}` is available that supports your dependency version
2. Contact the mod author about updating compatibility
3. Search for archived versions of the dependency
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModId")
            .AddValue<string>("ModName")
            .AddValue<string>("DependencyId")
            .AddValue<string>("VersionRange")
            .AddValue<string>("InstalledVersion")
        )
        .Finish();

    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate MissingDependencyWithVersionTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 4))
        .WithTitle("Missing Dependency With Version")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("'{ModName}' requires mod with ID '{DependencyId}' and Version '{Version}' or higher which is not installed")
        .WithDetails("""
The mod `{ModName}` requires a mod with the ID `{DependencyId}` to function, but `{DependencyId}` with version `{Version}` (or higher) is not installed.

### How to Resolve
1. Download version `{Version}` of mod `{DependencyId}`
2. Enable mod `{DependencyId}`

### Technical Details
This issue occurs when a mod needs a specific version of a dependency. Looking at `{ModName}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Current mod is `{ModName}` with id `{ModId}` -->
    <Id value="{ModId}"/>
    <Name value="{ModName}"/>
    <DependedModuleMetadatas>
        <!-- ❌ This dependency `{DependencyId}` (version `{Version}`) is not installed -->
        <DependedModuleMetadata id="{DependencyId}" order="LoadBeforeThis" version="{Version}" />
    </DependedModuleMetadatas>
</Module>
```

The issue can arise in these scenarios:

1. **Missing Installation**: The dependency was never installed
2. **Disabled Mod**: The dependency exists but isn't enabled in the loadout
3. **Incorrect Mod ID**: The dependency ID might be misspelled [if you are a mod author]
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModId")
            .AddValue<string>("ModName")
            .AddValue<string>("DependencyId")
            .AddValue<string>("Version")
        )
        .Finish();

    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate ModIncompatibleTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 5))
        .WithTitle("Mod Incompatible")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("'{ModName}' is not compatible with '{IncompatibleName}'")
        .WithDetails("""
The mod `{ModName}` cannot be used with mod `{IncompatibleName}` as they are marked as incompatible.

### How to Resolve
1. Disable either mod `{ModName}` or mod `{IncompatibleName}`
2. Look for alternative mods that are compatible with your loadout

### Technical Details
Looking at `{ModName}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Current mod is `{ModName}` with id `{ModId}` -->
    <Id value="{ModId}"/>
    <Name value="{ModName}"/>
    <DependedModuleMetadatas>
        <!-- ❌ This mod is incompatible with `{IncompatibleId}` which is `{IncompatibleName}` -->
        <DependedModuleMetadata id="{IncompatibleId}" incompatible="true" />
    </DependedModuleMetadatas>
</Module>
```

Common reasons for incompatibility:

1. Mods modify the same game features in conflicting ways
2. Technical limitations prevent the mods from working together
3. Known issues or bugs when used together
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModId")
            .AddValue<string>("ModName")
            .AddValue<string>("IncompatibleName")
            .AddValue<string>("IncompatibleId")
        )
        .Finish();

    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate ModVersionTooLowTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 6))
        .WithTitle("Outdated Dependency")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{ModName}' requires '{DependencyName}' version '{Version}' or newer but found older version '{InstalledVersion}'")
        .WithDetails("""
The mod `{ModName}` requires version `{Version}` or newer of `{DependencyName}` mod, but version `{InstalledVersion}` is installed.

### How to Resolve
1. Replace mod `{DependencyName}` version `{InstalledVersion}` with version `{Version}` or newer
2. Enable the compatible version of the `{DependencyName}` mod

### Technical Details
Looking at `{ModName}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Current mod is `{ModName}` with id `{ModId}` -->
    <Id value="{ModId}"/>
    <Name value="{ModName}"/>
    <DependedModuleMetadatas>
        <!-- 💡 The required mod `{DependencyName}` has id `{DependencyId}` -->
        <!-- ❌ `{DependencyId}` version `{InstalledVersion}` is older than minimum allowed version `{Version}` -->
        <DependedModuleMetadata id="{DependencyId}" version="{Version}" />
    </DependedModuleMetadatas>
</Module>
```

Common scenarios:

1. `{ModName}` was updated to a newer version without updating `{DependencyName}`
2. Multiple versions are installed and the wrong one is active

If you cannot find the right mod or version:

1. Contact the mod author of `{ModName}`
2. Downgrade `{ModName}` to an older version; if this happened right after mod update
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModId")
            .AddValue<string>("ModName")
            .AddValue<string>("DependencyId")
            .AddValue<string>("DependencyName")
            .AddValue<string>("Version")
            .AddValue<string>("InstalledVersion")
        )
        .Finish();

    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate BothRequiredAndIncompatibleTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 7))
        .WithTitle("Configuration Error")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("'{ModName}' has conflicting configuration: '{ConflictingId}' is marked as both required and incompatible")
        .WithDetails("""
The mod `{ModName}` has contradictory settings - it lists the ID `{ConflictingId}` as both a required dependency and an incompatible mod.

### How to Resolve
Contact the mod author to fix this configuration error in `{ModName}`.

### Technical Details
Looking at `{ModName}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Current mod is `{ModName}` with id `{ModId}` -->
    <Id value="{ModId}"/>
    <Name value="{ModName}"/>
    <DependedModules>
        <!-- 👇 `{ConflictingId}` is marked as a required dependency -->
        <DependedModule Id="{ConflictingId}" />
    </DependedModules>
    <DependedModuleMetadatas>
        <!-- 👇 `{ConflictingId}` is marked as incompatible, despite being required -->
        <DependedModuleMetadata id="{ConflictingId}" incompatible="true" />
    </DependedModuleMetadatas>
</Module>
```

❌ This is a configuration error as a mod cannot be both required and incompatible simultaneously.
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModId")
            .AddValue<string>("ModName")
            .AddValue<string>("ConflictingId")
        )
        .Finish();

    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate LoadOrderConflictTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 8))
        .WithTitle("Load Order Conflict")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("'{ModName}' has conflicting load order requirements for ID '{ConflictingId}'")
        .WithDetails("""
The mod `{ModName}` has contradictory load order requirements - it requires mod ID `{ConflictingId}` to load both before and after it.

### How to Resolve
Ensure `{ConflictingId}` does not have contradictory rules in `{ModName}`'s `SubModule.xml`.

### Not the Mod Author?
Contact the mod author to fix this configuration error.

### Technical Details
Looking at `{ModName}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Current mod is `{ModName}` with id `{ModId}` -->
    <Id value="{ModId}"/>
    <Name value="{ModName}"/>
    <DependedModuleMetadatas>
        <!-- 👇 `{ConflictingId}` is marked as `LoadBeforeThis` -->
        <DependedModuleMetadata id="{ConflictingId}" order="LoadBeforeThis" />
        <!-- 👇 `{ConflictingId}` is marked as `LoadAfterThis` -->
        <DependedModuleMetadata id="{ConflictingId}" order="LoadAfterThis" />
    </DependedModuleMetadatas>
</Module>
```

❌ This creates an impossible load order requirement as `{ConflictingId}` cannot load both before and after `{ModName}`.
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModId")
            .AddValue<string>("ModName")
            .AddValue<string>("ConflictingId")
        )
        .Finish();

    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate CircularDependencyTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 9))
        .WithTitle("Circular Dependency Conflict")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("Mods Are Stuck In a Loop: '{ModName}' and '{CircularDependencyName}'")
        .WithDetails("""
`{ModName}` and `{CircularDependencyName}` are creating a `circular dependency`.
This means each mod is waiting for the other to load first, causing a loop that stops both mods from working.

### How to Resolve
1. Contact the mod authors to resolve the circular dependency
2. Consider using only one of the mods until the issue is fixed
3. Look for alternative mods that provide similar functionality

### Technical Details
Looking at `{ModName}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Current mod is `{ModName}` with id `{ModId}` -->
    <Id value="{ModId}"/>
    <Name value="{ModName}"/>
    <DependedModuleMetadatas>
        <!-- 💡 The required mod `{CircularDependencyName}` has id `{CircularDependencyId}` -->
        <!-- 👇 `{ModName}` requests that `{CircularDependencyId}` loads first -->
        <DependedModuleMetadata id="{CircularDependencyId}" order="LoadBeforeThis" />
    </DependedModuleMetadatas>
</Module>
```

And looking at `{CircularDependencyId}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Other mod is `{CircularDependencyName}` with id `{CircularDependencyId}` -->
    <Id value="{CircularDependencyId}"/>
    <Name value="{CircularDependencyName}"/>
    <DependedModuleMetadatas>
        <!-- 👇 `{CircularDependencyId}` requests that `{ModName}` loads first -->
        <DependedModuleMetadata id="{ModId}" order="LoadBeforeThis" />
    </DependedModuleMetadatas>
</Module>
```

This creates an impossible situation because both mods can't load before the other mod.

### Hint For Mod Authors

Circular dependencies in mods can often be resolved by lifting out required shared functionality
into an additional mod.

Before (Conflict):

```
.---------.        .---------.
|  ModA   |------->|  ModB   |
|         |<-------|         |
'---------'        '---------'
```

Solution (Introduce ModC):

```
.---------.  .---------.
|  ModA   |  |  ModB   |
'---------'  '---------'
     \           /
      \         /
       v       v
    .-------------.
    |    ModC     |
    |-------------|
    | - Shared    |
    |   features  |
    | - Common    |
    |   assets    |
    '-------------'
```

In this case, you would:

- Identify features of `{ModId}` required by `{CircularDependencyId}`.
- Identify features of `{CircularDependencyId}` required by `{ModId}`.

And put all of these features in `ModC`, this way both mods can interoperate.
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModId")
            .AddValue<string>("ModName")
            .AddValue<string>("CircularDependencyId")
            .AddValue<string>("CircularDependencyName")
        )
        .Finish();

    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate ModMustLoadAfterTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 10))
        .WithTitle("Load Order Conflict")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{DependencyId}' should be loaded after '{ModName}'")
        .WithDetails("""
The mod `{ModName}` requires `{DependencyId}` to load after it, but the current load order has them in the wrong order.

### How to Resolve
Reorder your mods to ensure `{DependencyId}` loads after `{ModName}`.

### Technical Details
Looking at `{ModName}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Current mod is `{ModName}` with id `{ModId}` -->
    <Id value="{ModId}"/>
    <Name value="{ModName}"/>
    <DependedModuleMetadatas>
        <!-- ❌ `{DependencyId}` must load after this mod -->
        <DependedModuleMetadata id="{DependencyId}" order="LoadAfterThis" />
    </DependedModuleMetadatas>
</Module>
```

The current load order is incorrect. For example:

```
1. {DependencyId}  ❌
2. {ModName}
```

Should be:

```
1. {ModName}
2. {DependencyId}  ✅
```
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModId")
            .AddValue<string>("ModName")
            .AddValue<string>("DependencyId")
        )
        .Finish();

    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate ModMustLoadBeforeTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 11))
        .WithTitle("Load Order Conflict")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{DependencyId}' should be loaded before '{ModName}'")
        .WithDetails("""
The mod `{ModName}` requires `{DependencyId}` to load before it, but the current load order has them in the wrong order.

### How to Resolve
Reorder your mods to ensure `{DependencyId}` loads before `{ModName}`.

### Technical Details
Looking at `{ModName}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Current mod is `{ModName}` with id `{ModId}` -->
    <Id value="{ModId}"/>
    <Name value="{ModName}"/>
    <DependedModuleMetadatas>
        <!-- ❌ `{DependencyId}` must load before this mod -->
        <DependedModuleMetadata id="{DependencyId}" order="LoadBeforeThis" />
    </DependedModuleMetadatas>
</Module>
```

The current load order is incorrect. For example:

```
1. {ModId}
2. {DependencyId}  ❌
```

Should be:

```
1. {DependencyId}  ✅
2. {ModName}
```
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModId")
            .AddValue<string>("ModName")
            .AddValue<string>("DependencyId")
        )
        .Finish();

    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate ModBadConfigMissingIdTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 12))
        .WithTitle("Configuration Error")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("Mod with name '{ModName}' is missing its 'Id' field")
        .WithDetails("""
The mod `{ModName}` is missing its required ID field in its configuration.

The `Id` is a globally unique name for your mod that should not be shared with any other.
We recommend using a human friendly name; commonly the `Id` is just the mod name without spaces.

### How to Resolve
Add an `Id` field to the mod's `SubModule.xml`.

### Not the Mod Author?
Contact the mod author to fix this configuration error.

### Technical Details
Looking at the mod's `SubModule.xml`:

```xml
<Module>
    <!-- 👇❌ Required `Id` element is missing here -->
    <Name value="{ModName}"/>
</Module>
```

Should include an ID like this:
```xml
<Module>
    <!-- ✅ Example of a proper Id element -->
    <Id value="{ModNameNoSpaces}"/>
    <Name value="{ModName}"/>
</Module>
```

The Id field is required for:
- Unique identification of the mod
- Dependency management
- Load order resolution
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModName")
            .AddValue<string>("ModNameNoSpaces")
        )
        .Finish();

    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate ModBadConfigMissingNameTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 13))
        .WithTitle("Configuration Error")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("Mod with Id '{ModId}' is missing its 'Name' field")
        .WithDetails("""
The mod with ID `{ModId}` is missing its required `Name` field in its configuration.

The `Name` field is a friendly `Display Name` for the mod shown in `Mod Managers`.

### How to Resolve
Add a Name field to the mod's `SubModule.xml`.

### Not the Mod Author?
Contact the mod author to fix this configuration error.

### Technical Details
Looking at `{ModId}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Current mod ID -->
    <Id value="{ModId}"/>
    <!-- ❌ Required `Name` element is missing here -->
</Module>
```

Should include a Name like this:
```xml
<Module>
    <Id value="{ModId}"/>
    <!-- ✅ Example of a proper Name element -->
    <Name value="Example Mod Name"/>
</Module>
```

The Name field is required for:
- Display purposes
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModId")
        )
        .Finish();

    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate ModBadConfigNullDependencyTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 14))
        .WithTitle("Configuration Error")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("'{ModName}' has an empty dependency entry")
        .WithDetails("""
The mod `{ModName}` has an empty or invalid dependency entry in its configuration.

### How to Resolve
Fix the empty dependency entry in `{ModName}`'s `SubModule.xml`.

### Not the Mod Author?
Contact the mod author to fix this configuration error.

### Technical Details
Looking at `{ModName}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Current mod is `{ModName}` with id `{ModId}` -->
    <Id value="{ModId}"/>
    <Name value="{ModName}"/>
    <DependedModules>
        <!-- ❌ Empty/invalid dependency entry -->
        <DependedModule />
    </DependedModules>
</Module>
```

Should specify a dependency like this:
```xml
<Module>
    <Id value="{ModId}"/>
    <DependedModules>
        <!-- ✅ Example of a proper dependency entry -->
        <DependedModule Id="ExampleDependency" />
    </DependedModules>
</Module>
```

All dependency entries must include an Id to properly identify the required mod.
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModId")
            .AddValue<string>("ModName")
        )
        .Finish();

    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate ModBadConfigDependencyMissingIdTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 15))
        .WithTitle("Configuration Error")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("'{ModName}' has a dependency entry missing its 'id' field")
        .WithDetails("""
The mod `{ModName}` has a dependency entry that is missing its required `id` field.

### How to Resolve
Add the missing dependency ID in `{ModName}`'s `SubModule.xml`.

### Not the Mod Author?
Contact the mod author to fix this configuration error.

### Technical Details
Looking at `{ModName}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Current mod is `{ModName}` -->
    <Id value="{ModId}"/>
    <DependedModuleMetadatas>
        <!-- ❌ Missing 'id' attribute in dependency entry -->
        <DependedModuleMetadata version="v1.0.0" />
    </DependedModuleMetadatas>
</Module>
```

Should specify a dependency ID like this:
```xml
<Module>
    <Id value="{ModId}"/>
    <DependedModuleMetadatas>
        <!-- ✅ Example of a proper dependency entry with ID -->
        <DependedModuleMetadata id="ExampleDependency" version="v1.0.0" />
    </DependedModuleMetadatas>
</Module>
```

All dependency entries must include an ID to properly identify the required mod.
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModId")
            .AddValue<string>("ModName")
        )
        .Finish();
    
    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate MissingProtontricksForRedMod = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 16))
        .WithTitle("Missing Protontricks")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("Protontricks is required to run the game but is not present.")
        .WithDetails("""
Protontricks is required to run the game but is not present.

Refer to the {ProtontricksUri} for installation instructions.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<NamedLink>("ProtontricksUri")
        )
        .Finish();
    
    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate MissingBLSETemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 16))
        .WithTitle("'{ModName}' Requires Bannerlord Software Extender (BLSE)")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("'{ModName}' Requires Bannerlord Software Extender (BLSE) which is not installed")
        .WithDetails("""
'{ModName}' Requires Bannerlord Software Extender (BLSE) which is not installed.

### How to Resolve
1. Download and install {BLSELink}.
2. Enable BLSE.

### Technical Details
Looking at `{ModName}`'s `SubModule.xml`:

```xml
<Module>
    <!-- 👇 Current mod is `{ModName}` with id `{ModId}` -->
    <Id value="{ModId}"/>
    <Name value="{ModName}"/>
    <DependedModules>
        <!-- 💡 This mod requires `{DependencyId}` which is provided by BLSE -->
        <DependedModule Id="{DependencyId}" />
        <!-- ❌ BLSE is not installed -->
    </DependedModules>
</Module>
```
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ModId")
            .AddValue<string>("ModName")
            .AddValue<string>("DependencyId")
            .AddValue<NamedLink>("BLSELink")
        )
        .Finish();
    
    [DiagnosticTemplate, UsedImplicitly]
    internal static IDiagnosticTemplate MissingHarmonyTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, 17))
        .WithTitle("BLSE Requires Harmony which is not Installed")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("'Bannerlord Software Extender' (BLSE) requires 'Harmony' which is not installed")
        .WithDetails("""
'Bannerlord Software Extender' requires 'Harmony' to function, however 'Harmony' is not installed.

### How to Resolve
1. Download and install {HarmonyLink}
2. Enable Harmony
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<NamedLink>("HarmonyLink")
        )
        .Finish();
}
