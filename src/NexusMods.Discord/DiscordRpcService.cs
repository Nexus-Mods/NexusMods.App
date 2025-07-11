using DiscordRPC;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Sdk;

namespace NexusMods.Discord
{
    public class DiscordRpcService : IHostedService
    {
        private readonly DiscordRpcClient _client;
        private readonly string _applicationID = "1393166557101691101";
        private readonly ILogger<DiscordRpcService> _logger;

        public DiscordRpcService(ILogger<DiscordRpcService> logger)
        {
            _client = new DiscordRpcClient(_applicationID);
            _logger = logger;

            _client.OnReady += (sender, e) =>
            {
                _logger.LogInformation($"Connected to Discord as {e.User.Username}");
            };

            _client.OnConnectionEstablished += (sender, e) =>
            {
                _logger.LogInformation("Connection established with Discord.");
            };

            _client.OnPresenceUpdate += (sender, e) =>
            {
               _logger.LogInformation($"Presence updated. {e.Presence.ToString()}");
            };

            _client.OnError += (sender, e) =>
            {                 
                _logger.LogError($"Discord RPC Error: {e.Message}");
            };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                string version = ApplicationConstants.IsDebug ? "Debug Build" :  ApplicationConstants.Version.ToString(3) ?? "Unknown";

                _client.Initialize();
                _client.SetPresence(new RichPresence
                {
                    Details = "Modding with the Nexus Mods App",
                    State = $"Version: {version}",
                    Assets = new Assets
                    {
                        LargeImageKey = "nexusmods_logo",
                        LargeImageText = "Nexus Mods"
                    },
                    Buttons = new[]
                    {
                        new Button { Label="Get the app", Url="https://nexusmods.com/app" },
                    }
                });

            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to initialize Discord RPC: {ex.Message}");
            }

            _logger.LogInformation("Discord RPC service started successfully.");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _client.Dispose();
            _logger.LogInformation("Discord RPC service stopped.");
            return Task.CompletedTask;
        }

        public void SetPresence(RichPresence presence)
        {
            _client.SetPresence(presence);
            _logger.LogDebug($"Discord presence set: {presence.ToString()}");
        }

        public void ClearPresence()
        {
            _client.ClearPresence();
            _logger.LogInformation("Discord presence cleared.");
        }
    }
}
