## Mod Installation
Mod installation in the app happens in several phases
1. The user selects a mod to install
2. The app extracts the archive
3. Each file in the archive is analyzed by the `IFileAnalyzer` implementations
4. The app feeds the files (and metadata from analysis) to the `IModInstaller` implementation with the winning priority.
5. The `IModInstaller` returns a list of `AModFile` objects that determine how to install the mod into the various `GamePath` folders.

All analysis data is cached in the internal KV store so that the app doesn't have to re-analyze files every time it installs a mod.

### `IModInstaller` Interface
Now that you have your game registered with the DI system you may want to install a mod. The `IModInstaller` interface is used
as a way to register handlers for specific types of mods. The logic here is game specific, some games may support automatic installation via
detecting the file types and names. For example, Cyberpunk may know that `.archive` files are mods and can be installed by copying them to the mods folder. Or Skyrim installers
may know that `plugins/*.dll` files are skse plugins and should be copied to the skse plugins folder.
