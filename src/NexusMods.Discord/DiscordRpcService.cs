using System.Collections.ObjectModel;
using DiscordRPC;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Settings;
using NexusMods.Sdk;

namespace NexusMods.Discord
{
    public class DiscordRpcService : IHostedService
    {
        private DiscordRpcClient _client;
        private readonly string _applicationId = "1393166557101691101";
        private readonly ILogger<DiscordRpcService> _logger;
        private readonly ISettingsManager _settings;
        private bool _isEnabled = true;
        private bool _isInitialized = false;

        private readonly IReadOnlyDictionary<string, string> _assetKeys = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>
            {
                {"stardewvalley", "stardewvalley"},
                { "cyberpunk2077", "cyberpunk" },
            }
        );

        public DiscordRpcService(ILogger<DiscordRpcService> logger, ISettingsManager settingsManager)
        {
            _client = new DiscordRpcClient(_applicationId);
            _logger = logger;
            _settings = settingsManager;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //var settings = _settings.Get<DiscordSettings>();
            //_isEnabled = settings.EnableRichPresence;

            if (!_isEnabled)
            {
                _logger.LogInformation("Discord RPC is disabled in settings and was not started.");
                return Task.CompletedTask;
            }


            _client.OnReady += (_, e) =>
            {
                _logger.LogDebug($"Connected to Discord as {e.User.Username}");
            };

            _client.OnConnectionEstablished += (_, _) =>
            {
                _logger.LogDebug("Connection established with Discord.");
            };

            _client.OnError += (_, e) =>
            {
                _logger.LogError($"Discord RPC Error: {e.Message}");
            };

            if (_client.IsDisposed) { 
                _client = new DiscordRpcClient(_applicationId); 
                _logger.LogDebug("Recreated Discord RPC client due to previous disposal.");
            }

            try
            {
                _client.Initialize();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to initialize Discord RPC: {ex.Message}");
            }

            _logger.LogTrace("Discord RPC service started successfully.");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_isInitialized)
            {
                _logger.LogWarning("Discord RPC service was not initialized, skipping stop.");
            }
            else
            {
                _client.ClearPresence();
                _client.Dispose();
                _logger.LogInformation("Discord RPC service stopped.");
            }
            return Task.CompletedTask;
        }

        public void SetCustomPresence(RichPresence presence)
        {
            _client.SetPresence(presence);
            _logger.LogDebug($"Discord presence set: {presence.Details}");
        }

        public void SetDefaultPresence()
        {
            var version = ApplicationConstants.IsDebug 
                ? "Debug Build" 
                : ApplicationConstants.Version.ToString(3) ?? "Unknown";
            
            _client.SetPresence(new RichPresence
            {
                Details = "Modding with the Nexus Mods App",
                State = $"Version: {version}",
                Assets = new Assets
                {
                    LargeImageKey = "nexusmods_logo",
                    LargeImageText = "Nexus Mods",
                },
            });
        }

        public void SetGamePresence(string gameName, string gameDomain, int modCount)
        {
            if (!_isInitialized || !_isEnabled)
            {
                _logger.LogWarning("Discord RPC is not initialized or enabled, cannot set game presence.");
                return;
            }
            
            var largeImageKey = "nexusmods_logo";
            
            if (_assetKeys.TryGetValue(gameDomain, out var gameArtKey))
            {
                largeImageKey = gameArtKey;
            }

            _logger.LogInformation($"Setting Discord presence for game: {gameName}, Domain: {gameDomain}, Mods: {modCount}");

            _client.SetPresence(new RichPresence
            {
                Details = $"Modding {gameName}",
                State = modCount > 0 ? $"{modCount} mods installed" : "",
                Assets = new Assets
                {
                    LargeImageKey = largeImageKey,
                    LargeImageText = largeImageKey == "nexusmods_logo"  ? "Nexus Mods" : gameName,
                    SmallImageKey = largeImageKey == "nexusmods_logo" ? null : "nexusmods_logo",
                    SmallImageText = largeImageKey == "nexusmods_logo" ? null : "Nexus Mods",
                },
            });
        }
    }
}

public interface IDiscordRpcService
{
    void SetGamePresence(string gameName, string gameDomain, int modCount);

    void SetCustomPresence(RichPresence presence);

    void SetDefaultPresence();

    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
