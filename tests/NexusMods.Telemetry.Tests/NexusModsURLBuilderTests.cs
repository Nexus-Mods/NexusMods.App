using DynamicData.Kernel;
using FluentAssertions;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Telemetry;
using Xunit;

namespace NexusMods.Telemetry.Tests;

public class NexusModsUrlBuilderTests
{
    private const string StardewValley = "stardewvalley";

    [Theory]
    [InlineData(6672467ul, "https://www.nexusmods.com/users/6672467")]
    public void Test_GetProfileUri(ulong userId, string expected)
    {
        var actual = NexusModsUrlBuilder.GetProfileUri(UserId.From(userId), source: null).ToString();
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(StardewValley, "https://www.nexusmods.com/games/stardewvalley")]
    public void Test_GetGameUri(string gameDomain, string expected)
    {
        var actual = NexusModsUrlBuilder.GetGameUri(GameDomain.From(gameDomain), source: null).ToString();
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(StardewValley, 2400, "https://www.nexusmods.com/stardewvalley/mods/2400")]
    public void Test_GetModUri(string gameDomain, uint modId, string expected)
    {
        var actual = NexusModsUrlBuilder.GetModUri(GameDomain.From(gameDomain), ModId.From(modId), source: null).ToString();
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(StardewValley, 2400, 128328, false, "https://www.nexusmods.com/stardewvalley/mods/2400?tab=files&file_id=128328&nmm=0")]
    [InlineData(StardewValley, 2400, 128328, true, "https://www.nexusmods.com/stardewvalley/mods/2400?tab=files&file_id=128328&nmm=1")]
    public void Test_GetFileDownloadUri(string gameDomain, uint modId, uint fileId, bool useNxmLink, string expected)
    {
        var actual = NexusModsUrlBuilder.GetFileDownloadUri(GameDomain.From(gameDomain), ModId.From(modId), FileId.From(fileId), useNxmLink, source: null).ToString();
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(StardewValley, "tckf0m", null, "https://www.nexusmods.com/games/stardewvalley/collections/tckf0m")]
    [InlineData(StardewValley, "tckf0m", 80ul, "https://www.nexusmods.com/games/stardewvalley/collections/tckf0m/revisions/80")]
    public void Test_GetCollectionUri(string gameDomain, string collectionSlug, ulong? revisionNumber, string expected)
    {
        var actual = NexusModsUrlBuilder.GetCollectionUri(GameDomain.From(gameDomain), CollectionSlug.From(collectionSlug), revisionNumber.HasValue ? RevisionNumber.From(revisionNumber.Value) : default(Optional<RevisionNumber>), source: null).ToString();
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(StardewValley, "tckf0m", null, "https://www.nexusmods.com/games/stardewvalley/collections/tckf0m/bugs")]
    [InlineData(StardewValley, "tckf0m", 80ul, "https://www.nexusmods.com/games/stardewvalley/collections/tckf0m/revisions/80/bugs")]
    public void Test_GetCollectionBugsUri(string gameDomain, string collectionSlug, ulong? revisionNumber, string expected)
    {
        var actual = NexusModsUrlBuilder.GetCollectionBugsUri(GameDomain.From(gameDomain), CollectionSlug.From(collectionSlug), revisionNumber.HasValue ? RevisionNumber.From(revisionNumber.Value) : default(Optional<RevisionNumber>), source: null).ToString();
        actual.Should().Be(expected);
    }
}
