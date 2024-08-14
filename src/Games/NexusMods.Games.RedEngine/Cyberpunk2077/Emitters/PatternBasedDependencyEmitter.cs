using System.Text.RegularExpressions;
using Avalonia.Controls;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

public class PatternBasedDependencyEmitter : ILoadoutDiagnosticEmitter
{
    /// <summary>
    /// Search patterns indexed by extension for quick lookup.
    /// </summary>
    private readonly Dictionary<Extension, IGrouping<Extension, (Pattern pattern, DependantSearchPattern searchPattern)>> _byExtension;

    /// <summary>
    /// Patterns and their dependency paths in a hashset for quick lookup.
    /// </summary>
    private readonly IEnumerable<(HashSet<GamePath> Paths, Pattern Pattern)> _dependencies;
    
    /// <summary>
    /// A union of all dependency files.
    /// </summary>
    private readonly HashSet<GamePath> _dependencyFiles;

    private readonly IFileStore _fileStore;

    public PatternBasedDependencyEmitter(Pattern[] patterns, IServiceProvider provider)
    {
        _fileStore = provider.GetRequiredService<IFileStore>();
        _byExtension = patterns.SelectMany(pattern => pattern.DependantSearchPatterns.Select(searchPattern => (searchPattern.Extension, pattern, searchPattern)))
            .GroupBy(tuple => tuple.Extension, tuple => (tuple.pattern, tuple.searchPattern))
            .ToDictionary(group => group.Key);;

        _dependencies = patterns.Select(pattern => (pattern.DependencyPaths.ToHashSet(), pattern)).ToArray();
        _dependencyFiles = _dependencies.SelectMany(dependency => dependency.Paths).ToHashSet();
    }
    
    
    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        // TODO: use a sorted index scan here to speed this up
        var allFiles = loadout.Items.OfTypeLoadoutItemWithTargetPath()
            .Where(item => _dependencyFiles.Contains(item.TargetPath))
            .ToHashSet();
        
        var enabledFiles = allFiles
            .Where(f => f.AsLoadoutItem().GetThisAndParents().All(p => p.IsEnabled()))
            .ToHashSet();
        
        var installedDependencies = _dependencies
            .Where(dependency => dependency.Paths.All(path => allFiles.All(f => f.TargetPath == path)))
            .ToDictionary(pattern => pattern.Pattern.DependencyName);
        
        var enabledDependencies = _dependencies
            .Where(dependency => dependency.Paths.All(path => enabledFiles.All(f => f.TargetPath == path)))
            .ToDictionary(pattern => pattern.Pattern.DependencyName);
        
        var requiredMods = new Dictionary<string, (LoadoutItemWithTargetPath.ReadOnly File, Pattern Pattern, Optional<string> ExampleUsage)>();
        
        
        foreach (var file in loadout.Items.OfTypeLoadoutItemWithTargetPath())
        {
            if (!_byExtension.TryGetValue(file.TargetPath.Item3.Extension, out var patterns))
                continue;

            foreach (var (pattern, searchPattern) in patterns)
            {
                if (requiredMods.ContainsKey(pattern.DependencyName))
                    continue;
                
                if (((GamePath)file.TargetPath).InFolder(searchPattern.Path))
                {
                    if (searchPattern.Regex.HasValue && file.TryGetAsLoadoutFile(out var loadoutFile))
                    {
                        if (await SearchContents(loadoutFile, searchPattern.Regex.Value))
                        {
                            requiredMods.Add(pattern.DependencyName, (file, pattern, searchPattern.Regex.Value.ToString()));
                        }
                    }
                    else
                    {
                        requiredMods.Add(pattern.DependencyName, (file, pattern, Optional<string>.None));
                    }
                }
            }
        }
        
        foreach (var (requiredMod, row) in requiredMods)
        {
            if (enabledDependencies.ContainsKey(requiredMod))
            {
                continue;
            }
            else if (installedDependencies.TryGetValue(requiredMod, out var group))
            {
               var (dependencyPaths, pattern) = group;
               var parent = row.File.AsLoadoutItem().Parent;
               var disabledGroup = loadout.Items.OfTypeLoadoutItemGroup()
                   .Where(group =>
                       {
                           // This will be slow and break if the user has multiple groups that have overlapping dependencies, but it's good enough for now.
                           var groupPaths = group.Children.OfTypeLoadoutItemWithTargetPath().Select(p => (GamePath)p.TargetPath).ToHashSet();
                           return dependencyPaths.All(path => groupPaths.Contains(path));
                       }
                   ).First();
               yield return Diagnostics.CreateDisabledGroupDependency(parent.ToReference(loadout), disabledGroup.ToReference(loadout));
            }
            else
            {
                var parent = row.File.AsLoadoutItem().Parent;
                var downloadLink = new NamedLink("Nexus Mods", new($"https://www.nexusmods.com/cyberpunk2077/mods/{row.Pattern.ModId}"));
                yield return Diagnostics.CreateMissingModWithKnownNexusUri(parent.ToReference(loadout), requiredMod, downloadLink);
            }
        }
    }
    
    public async Task<bool> SearchContents(LoadoutFile.ReadOnly file, Regex regex)
    {
        try
        {
            var data = await _fileStore.GetFileStream(file.Hash);
            var content = await data.ReadAllTextAsync();
            return regex.IsMatch(content);
        }
        catch (Exception e)
        {
            return false;
        }
    }
}
