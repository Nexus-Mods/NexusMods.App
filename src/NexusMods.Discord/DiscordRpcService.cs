using System.Collections.ObjectModel;
using DiscordRPC;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Sdk;

namespace NexusMods.Discord
{
    public class DiscordRpcService : IHostedService
    {
        private readonly DiscordRpcClient _client;
        private readonly string _applicationId = "1393166557101691101";
        private readonly ILogger<DiscordRpcService> _logger;
        private CancellationTokenSource? _periodicUpdateTokenSource;
        private string _lastGameName = "";

        private readonly IReadOnlyDictionary<string, string> _assetKeys = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>
            {
                {"stardewvalley", "stardewvalley"},
                { "cyberpunk2077", "cyberpunk" },
            }
        );

        public DiscordRpcService(ILogger<DiscordRpcService> logger)
        {
            _client = new DiscordRpcClient(_applicationId);
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
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

            try
            {
                _client.Initialize();
                SetDefaultPresence();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to initialize Discord RPC: {ex.Message}");
            }

            _logger.LogTrace("Discord RPC service started successfully.");

            _periodicUpdateTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _ = Task.Run(async () =>
                {
                    try
                    {
                        while (!_periodicUpdateTokenSource.Token.IsCancellationRequested)
                        {
                            if (_lastGameName != "")
                            {
                                if (_lastGameName == "Stardew Valley")
                                {
                                    _logger.LogInformation("Discord RPC set to Cyberpunk");
                                    SetGamePresence("Cyberpunk 2077", "cyberpunk2077", 9);
                                    _lastGameName = "Cyberpunk 2077";
                                }
                                else
                                {
                                    _logger.LogInformation("Discord RPC set to Unknown");
                                    SetGamePresence("Unknown Game", "unknown", 0);
                                    _lastGameName = "";
                                }
                            }
                            else
                            {
                                _logger.LogInformation("Discord RPC set to Stardew");
                                SetGamePresence("Stardew Valley", "stardewvalley", 222);
                                _lastGameName = "Stardew Valley";
                            }
                            
                            await Task.Delay(TimeSpan.FromSeconds(15), _periodicUpdateTokenSource.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Discord RPC periodic update task cancelled.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Discord RPC periodic update task failed: {ex.Message}");
                    }
                }, _periodicUpdateTokenSource.Token
            );

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _client.Dispose();
            _logger.LogInformation("Discord RPC service stopped.");
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
            var largeImageKey = "nexusmods_logo";
            
            if (_assetKeys.TryGetValue(gameDomain, out var gameArtKey))
            {
                largeImageKey = gameArtKey;
            }
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

        public void ClearPresence()
        {
            _client.ClearPresence();
            _logger.LogInformation("Discord presence cleared.");
        }
    }
}
