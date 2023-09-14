# Persistence/Restore Functionality for Advanced Installer

Manual installs need to be remembered across different deployments to avoid the user having to remap them every time
between installations.

## Context and Problem Statement

After manually installing a mod via the Advanced Installer, the application needs to save the details of the user's
last set of choices made in the Installer.

This is such that, when the application needs to perform a re-deployment occurs, the end user does not have to go through
the manual installation steps again.

Note: Re-deployment can happen due to a variety of factors, such as a mod deployed before the Advanced Installed mod
being removed from a loadout. Not all re-deployments are due to the user's direct actions/expectations. Therefore a
persistence mechanism here is crucial.

## Decision Drivers & Requirements

- Ease of Use: A key aspect of the user experience is minimizing repetitive tasks.
  Having to re-enter manual mappings is ***slow, and error-prone***.

- Maintainability: The persistence system should be easy to reuse in other installers that are similar to the Advanced
  Installer (i.e. installers with multiple 'Variants'), such as FOMOD.

## Decision Outcome

Saving the mappings to our Data Store (`IDataStore`) is the preferred approach. We will
associate the settings with each individual Mod within a loadout; more details on that later.

### Consequences

- Good for data integrity, as databases have mechanisms for ensuring data reliability.
- Cleanly integrates with our existing loadout system.

### Implementation Details / Notes

#### Design: Save Format (Proposed)

Create a class named `AdvancedInstallSettings` (or similar), used to serialize and deserialize deployment
information across runs.

```csharp
[JsonName("NexusMods.Games.AdvancedInstaller.AdvancedInstallSettings")]
public class AdvancedInstallSettings : Entity
{
    public Dictionary<string, string> ArchiveToOutputMap { get; init; } = new();
}
```

And extend the existing `DeploymentData` struct to include methods for serialization and deserialization.
This would allow for easy storage into and retrieval from a local database.

```csharp
public class DeploymentDataExtensions
{
    /* Pretend there's existing code */

    /// <summary>
    /// Serializes the current DeploymentData instance to a Database Entity.
    /// </summary>
    /// <returns>A serialized string representation of the object.</returns>
    public static AdvancedInstallSettings Serialize(this DeploymentData data)
    {
        // Implementation //
    }

    /// <summary>
    /// Deserializes the Database Entity instance.
    /// </summary>
    /// <param name="serializedData">The data to deserialize.</param>
    public static void Deserialize(AdvancedInstallSettings serializedData)
    {
        // Implementation //
    }
}
```

Note: We convert between `Dictionary<RelativePath, GamePath>` and `Dictionary<string, string>` during the serialize/deserialize operations.
This might have minor overhead, though zero cost unsafe cast might be possible.

#### Design: Save Format Storage

Note the `IModInstaller` interface method from within which the Advanced Installer operates during deployment:

```csharp
/// <summary>
/// Finds all mods inside the provided archive.
/// </summary>
/// <param name="gameInstallation">The game installation.</param>
/// <param name="baseModId">The base mod id.</param>
/// <param name="srcArchiveHash">Hash of the source archive.</param>
/// <param name="archiveFiles">Files from the archive.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns></returns>
public ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
    GameInstallation gameInstallation,
    ModId baseModId,
    Hash srcArchiveHash,
    EntityDictionary<RelativePath, AnalyzedFile> archiveFiles,
    CancellationToken cancellationToken = default);
```

The `ModId` field named `baseModId` is actually a bit more than it seems, note its documentation:

```
/// A unique identifier for the mod for use within the data store/database.
/// These IDs are assigned to mods upon installation (i.e. when a mod is
/// added to a loadout), or when a tool generates some files after running.
```

In other words, `ModId` is unique to a mod deployment/installation within a given
loadout; and as such as a perfect candidate for usage as key within the data store.

However, it must be noted that multiple installers can run over a mod, thus our database key
needs to consist of a `ModId` + `InstallerId` tuple. This will require a custom ID in `NexusMods.DataModel.Abstractions.Ids`,
which will be 16+ bytes provided it is expected (under current system) that both items are GUIDs.

A new item type would also need allocating in the `DataModel`.

#### Design: Save Format Reuse

The proposed save data can be reused, across other types of deployment that are similar in nature to `Advanced Installer`
such as FOMOD deployment.

The `Advanced Installer` settings structure is intended for reuse through the use of composition, as a structure embedded
within another structure, for example:

```csharp
[JsonName("NexusMods.Games.AdvancedInstaller.FOMODInstallSettings")]
public class FOMODInstallSettings : Entity
{
    public AdvancedInstallSettings FileCopySettings { get; init; } = new();
    /* FOMOD Specific Cached Data Below Here */
}
```

In the case of other variant type installers like FOMOD, the list of 'instructions' to perform can be stored and serialized
as `AdvancedInstallSettings`.

When re-deployment occurs, we check for any possible situations where the previous user setting might be invalidated.
(e.g. In the case of FOMOD, if a new file which a FOMOD has an optional dependency on now exists when it has not existed, or vice versa,
it may be desirable to ask the user for feedback.)

If none of these conditions occur, we can simply convert `AdvancedInstallSettings` directly to instructions
(same way as with `AdvancedInstaller`), and carry on with re-deployment. This way we reuse the code across multiple
advanced-installer like implementations.
