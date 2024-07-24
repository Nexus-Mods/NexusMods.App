using NexusMods.App.GarbageCollection.Errors;
using NexusMods.App.GarbageCollection.Structs;
namespace NexusMods.App.GarbageCollection.Utilities;

internal static class ThrowHelpers
{
    internal static void ThrowUnknownFileException(Hash hash) => throw new UnknownFileException(hash);
}
