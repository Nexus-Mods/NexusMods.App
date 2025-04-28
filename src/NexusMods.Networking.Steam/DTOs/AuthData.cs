using System.Text;
using System.Text.Json;

namespace NexusMods.Networking.Steam.DTOs;

internal class AuthData
{
    public required string Username { get; init; }
    public required string RefreshToken { get; init; }

    public byte[] Save()
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this));
    }
    
    public static AuthData Load(byte[] data)
    {
        return JsonSerializer.Deserialize<AuthData>(Encoding.UTF8.GetString(data))!;
    }
}
