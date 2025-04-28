using NexusMods.Abstractions.Steam;
using NexusMods.Paths;

namespace NexusMods.Networking.Steam;


internal class AppDirectoryAuthStorage(IFileSystem fileSystem) : IAuthStorage
{
    private readonly AbsolutePath _storagePath = fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory)
                                                 / (fileSystem.OS.IsOSX ? "NexusMods_App" : "NexusMods.App")
                                                 / "steam/auth";
    
    public async Task<(bool Success, byte[] Data)> TryLoad()
    {
        try
        {
            if (!_storagePath.FileExists)
                return (false, []);

            return (true, await _storagePath.ReadAllBytesAsync());
        }
        catch
        {
            return (false, []);
        }

    }

    public async Task SaveAsync(byte[] data)
    {
        if (!_storagePath.Parent.DirectoryExists())
            _storagePath.Parent.CreateDirectory();
        await _storagePath.WriteAllBytesAsync(data);
    }
}
