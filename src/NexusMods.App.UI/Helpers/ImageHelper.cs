using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Helpers;

/// <summary>
/// Shared utility for loading game images and creating IconValues.
/// </summary>
public static class ImageHelper
{
    /// <summary>
    /// Loads a game icon as a Bitmap from the game's icon stream.
    /// </summary>
    /// <param name="game">The game to load the icon for.</param>
    /// <param name="width">The width to decode the image to.</param>
    /// <param name="logger">Logger for error handling.</param>
    /// <returns>A Bitmap if successful, null otherwise.</returns>
    public static async ValueTask<Bitmap?> LoadGameIconAsync(IGame? game, int width, ILogger logger)
    {
        if (game is null)
            return null;

        try
        {
            await using var iconStream = await game.Icon.GetStreamAsync();
            return Bitmap.DecodeToWidth(iconStream, width);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "While loading game image for {GameName}", game.DisplayName);
            return null;
        }
    }

    /// <summary>
    /// Creates an IconValue from a Bitmap with fallback handling.
    /// </summary>
    /// <param name="bitmap">The bitmap to convert to an IconValue.</param>
    /// <param name="fallback">The fallback IconValue to use if bitmap is null.</param>
    /// <returns>An IconValue containing the bitmap or the fallback.</returns>
    public static IconValue CreateIconValueFromBitmap(Bitmap? bitmap, IconValue fallback)
    {
        if (bitmap != null)
            return new AvaloniaImage(bitmap);
        
        return fallback;
    }
}
