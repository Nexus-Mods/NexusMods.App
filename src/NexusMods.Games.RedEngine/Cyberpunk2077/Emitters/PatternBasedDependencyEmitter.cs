using System.Runtime.CompilerServices;
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
using NexusMods.Abstractions.Telemetry;
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
    private readonly (HashSet<GamePath> Paths, Pattern Pattern)[] _dependencies;
    
    /// <summary>
    /// A union of all dependency files.
    /// </summary>
    private readonly HashSet<GamePath> _dependencyFiles;

    private readonly IFileStore _fileStore;

    internal record MatchingDependency
    {
        public required LoadoutItemWithTargetPath.ReadOnly File { get; init; } 
        public required Pattern Pattern { get; init; }
        public required DependantSearchPattern SearchPattern { get; init; }
        public required Optional<string> MatchingSegment { get; init; }
        public required int StartingLineNumber { get; init; }
    }

    public PatternBasedDependencyEmitter(Pattern[] patterns, IServiceProvider provider)
    {
        _fileStore = provider.GetRequiredService<IFileStore>();
        _byExtension = patterns.SelectMany(pattern => pattern.DependantSearchPatterns.Select(searchPattern => (searchPattern.Extension, pattern, searchPattern)))
            .GroupBy(tuple => tuple.Extension, tuple => (tuple.pattern, tuple.searchPattern))
            .ToDictionary(group => group.Key);;

        _dependencies = patterns.Select(pattern => (pattern.DependencyPaths.ToHashSet(), pattern)).ToArray();
        _dependencyFiles = _dependencies.SelectMany(dependency => dependency.Paths).ToHashSet();
    }
    
    
    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // TODO: use a sorted index scan here to speed this up
        
        // All loadout items that are parts of dependency mods
        var allFiles = loadout.Items.OfTypeLoadoutItemWithTargetPath()
            .Where(item => _dependencyFiles.Contains(item.TargetPath))
            .ToHashSet();
        
        // Just the files that are part of enabled mods
        var enabledFiles = allFiles
            .Where(f => f.AsLoadoutItem().GetThisAndParents().All(p => p.IsEnabled()))
            .ToHashSet();
        
        // Just the paths, as a hashset for quick lookup
        var allPaths = allFiles.Select(f => (GamePath)f.TargetPath).ToHashSet();
        var enabledPaths = enabledFiles.Select(f => (GamePath)f.TargetPath).ToHashSet();
        
        // The installed dependencies are those where all paths are present
        var installedDependencies = _dependencies
            .Where(dependency => dependency.Paths.All(path => allPaths.Contains(path)))
            .ToDictionary(pattern => pattern.Pattern.DependencyName);
        
        // The enabled dependencies are those where all paths are present and the mod is enabled
        var enabledDependencies = _dependencies
            .Where(dependency => dependency.Paths.All(path => enabledPaths.Contains(path)))
            .ToDictionary(pattern => pattern.Pattern.DependencyName);
        
        
        var requiredMods = new Dictionary<string, MatchingDependency>();
        
        // Loop through all loadout items that match one of the patterns we have
        foreach (var file in loadout.Items.OfTypeLoadoutItemWithTargetPath())
        {
            if (!_byExtension.TryGetValue(file.TargetPath.Item3.Extension, out var patterns))
                continue;

            // Ignore disabled files
            if (!file.AsLoadoutItem().GetThisAndParents().All(f => f.IsEnabled()))
                continue;
            
            // Check if the file is part of a dependency mod
            foreach (var (pattern, searchPattern) in patterns)
            {
                if (requiredMods.ContainsKey(pattern.DependencyName))
                    continue;
                
                if (((GamePath)file.TargetPath).InFolder(searchPattern.Path))
                {
                    // If there is an attached regex, we need to search the file contents
                    if (searchPattern.Regex.HasValue && file.TryGetAsLoadoutFile(out var loadoutFile))
                    {
                        var (isMatch, matchingSegment, startingLineNumber) = await SearchContents(loadoutFile, searchPattern.Regex.Value);
                        if (isMatch)
                        {
                            requiredMods.Add(pattern.DependencyName, new MatchingDependency
                            {
                                File = file,
                                Pattern = pattern,
                                SearchPattern = searchPattern,
                                MatchingSegment = matchingSegment,
                                StartingLineNumber = startingLineNumber,
                            });
                        }
                    }
                    else
                    {
                        requiredMods.Add(pattern.DependencyName, new MatchingDependency
                        {
                            File = file,
                            Pattern = pattern,
                            SearchPattern = searchPattern,
                            MatchingSegment = Optional<string>.None,
                            StartingLineNumber = 0,
                        });
                    }
                }
            }
        }
        
        // Output diagnostics for all required mods that are not installed
        foreach (var (requiredMod, row) in requiredMods)
        {
            // Check if the dependency is already installed, enabled or disabled
            if (enabledDependencies.ContainsKey(requiredMod))
            {
                continue;
            }
            else if (installedDependencies.TryGetValue(requiredMod, out var group))
            {
               // Disabled Dependency Group 
               var (dependencyPaths, pattern) = group;
               var parent = row.File.AsLoadoutItem().Parent;
               
               // Group that is disabled
               var disabledGroup = loadout.Items.OfTypeLoadoutItemGroup()
                   .Where(group =>
                       {
                           // This will be slow and break if the user has multiple groups that have overlapping dependencies, but it's good enough for now.
                           var groupPaths = group.Children.OfTypeLoadoutItemWithTargetPath().Select(p => (GamePath)p.TargetPath).ToHashSet();
                           return dependencyPaths.All(path => groupPaths.Contains(path));
                       }
                   ).First();
               if (row.MatchingSegment.HasValue)
               {
                   yield return Diagnostics.CreateDisabledGroupDependencyWithStringSegment(parent.ToReference(loadout), disabledGroup.ToReference(loadout), requiredMod,
                       pattern.Explanation, row.File.TargetPath, row.SearchPattern.Path,
                       row.SearchPattern.Extension, row.MatchingSegment.Value, row.StartingLineNumber
                   );
               }
               else
               {
                   yield return Diagnostics.CreateDisabledGroupDependency(parent.ToReference(loadout), disabledGroup.ToReference(loadout), requiredMod,
                       pattern.Explanation, row.File.TargetPath, row.SearchPattern.Path,
                       row.SearchPattern.Extension
                   );
                   
               }
            }
            else
            {
                // Missing mod
                var parent = row.File.AsLoadoutItem().Parent;
                var downloadLink = new NamedLink("Nexus Mods", NexusModsUrlBuilder.GetModUri(Cyberpunk2077Game.StaticDomain, row.Pattern.ModId, campaign: NexusModsUrlBuilder.CampaignDiagnostics));
                if (row.MatchingSegment.HasValue)
                {
                    yield return Diagnostics.CreateMissingModWithKnownNexusUriWithStringSegment(
                        parent.ToReference(loadout), requiredMod, downloadLink, row.Pattern.Explanation,
                        row.File.TargetPath, row.SearchPattern.Path, row.SearchPattern.Extension, row.MatchingSegment.Value, row.StartingLineNumber);
                }
                else
                {
                    yield return Diagnostics.CreateMissingModWithKnownNexusUri(
                        parent.ToReference(loadout), requiredMod, downloadLink, row.Pattern.Explanation,
                        row.File.TargetPath, row.SearchPattern.Path, row.SearchPattern.Extension);
                    
                }
            }
        }
    }
    
    public async Task<(bool isMatch, string matchingSegment, int startingLineNumber)> SearchContents(LoadoutFile.ReadOnly file, Regex regex)
    {
        try
        {
            var data = await _fileStore.GetFileStream(file.Hash);
            var content = await data.ReadAllTextAsync();
        
            var match = regex.Match(content);
            if (!match.Success)
            {
                return (false, "", 0);
            }

            // Find the first \n before and after the matching segment
            var start = content.LastIndexOf('\n', match.Index) + 1;
            var end = content.IndexOf('\n', match.Index + match.Length);
            if (end == -1) end = content.Length;

            var matchingSegment = content.Substring(start, end - start);

            // Calculate the starting line number
            var startingLineNumber = content.Substring(0, start).Count(c => c == '\n') + 1;

            return (true, matchingSegment, startingLineNumber);
        }
        catch (Exception e)
        {
            return (false, "", 0);
        }
    }
}
