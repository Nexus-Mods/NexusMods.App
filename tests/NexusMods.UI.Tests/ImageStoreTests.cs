using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.UI.Tests;

public class ImageStoreTests : AUiTest
{
    private readonly TemporaryFileManager _temporaryFileManager;

    public ImageStoreTests(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
    }

    [Fact]
    public async Task SimpleTest()
    {
        await using var tmp = _temporaryFileManager.CreateFolder();
        await using var imageStore = new ImageStore(tmp);

        var bitmap = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));
        await imageStore.Store(EntityId.From(1), bitmap);

        var retrieved = await imageStore.Retrieve(EntityId.From(1));
        retrieved.Should().NotBeNull();
    }

    [Fact]
    public async Task ReOpenTest()
    {
        await using var tmp = _temporaryFileManager.CreateFolder();
        await using (var imageStore = new ImageStore(tmp))
        {
            var bitmap = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));
            await imageStore.Store(EntityId.From(1), bitmap);
        }

        await using (var imageStore = new ImageStore(tmp))
        {
            var retrieved = await imageStore.Retrieve(EntityId.From(1));
            retrieved.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task ParallelNoMixedTest()
    {
        await using var tmp = _temporaryFileManager.CreateFolder();
        await using var imageStore = new ImageStore(tmp);

        var bitmap = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));
        var entityIds = Enumerable.Range(start: 0, count: 100).Select(static x => EntityId.From((ulong)x)).ToArray();

        // parallel writes, no reads
        await Parallel.ForAsync(fromInclusive: 0, toExclusive: entityIds.Length, async (i, _) =>
        {
            var entityId = entityIds[i];
            await imageStore.Store(entityId, bitmap);
        });

        // parallel reads, no writes
        await Parallel.ForAsync(fromInclusive: 0, toExclusive: entityIds.Length, async (i, _) =>
        {
            var entityId = entityIds[i];
            var retrieved = await imageStore.Retrieve(entityId);
            retrieved.Should().NotBeNull();
        });
    }
}
