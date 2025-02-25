// using DynamicData.Kernel;
// using JetBrains.Annotations;
// using NexusMods.Abstractions.Diagnostics.Values;
// using NexusMods.Paths;
//
// namespace NexusMods.Games.UnrealEngine.Interfaces;
//
// /// <summary>
// /// Interface for working with CUE4Parse.
// /// </summary>
// [PublicAPI]
// public interface IPakParserService : IDisposable
// {
//     /// <summary>
//     /// Fetches details for mods using their IDs.
//     /// </summary>
//     public Task<IReadOnlyDictionary<string, SMAPIWebApiMod>> GetModDetails(
//         IOSInformation os,
//         ISemanticVersion gameVersion,
//         ISemanticVersion smapiVersion,
//         string[] smapiIDs
//     );
// }
//
// public record SMAPIWebApiMod
// {
//     public required string UniqueId { get; init; }
//
//     public required string? Name { get; init; }
// }
//
// public static class SMAPIWebApiExtensions
// {
//     public static NamedLink GetAssetFiles(this IReadOnlyDictionary<string, SMAPIWebApiMod> mods, string id, NamedLink defaultValue)
//     {
//         var mod = mods.GetValueOrDefault(id);
//         if (mod is null) return defaultValue;
//
//         var link = mod.NexusModsLink;
//         return link.HasValue ? link.Value : defaultValue;
//     }
// }
