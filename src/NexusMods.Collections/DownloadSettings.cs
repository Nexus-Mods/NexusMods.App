using NexusMods.Sdk.Settings;

namespace NexusMods.Collections;

public class DownloadSettings : ISettings
{
    // Use parallel downloads with concurrency limit
    // The concurrency limit is set as follows:
    // - I (Sewer56) have a 15.4ms ping to api.nexusmods.com
    // - A request for download link takes ~80-140ms (v1 API) + ~90-100ms to initiate download using the link. 
    //    - That includes HTTP handshake, SSL handshake, etc.
    //    - For API requests, handshakes are cached, but not for CDN; hence roughly equivalent times.
    // - Now: `240 / x = 15.4`, solve for `x`.
    //         x = 240 / 15.4 = 15.58
    // 
    // So we round that up to a nice 16 concurrency level.
    // Should be good.
    public int MaxParallelDownloads { get; set; } = Math.Max(Environment.ProcessorCount, 16);

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.ConfigureProperty(
            x => x.MaxParallelDownloads,
            new PropertyOptions<DownloadSettings,int>
            {
                Section = Sections.General,
                DisplayName = "Max Parallel Downloads",
                DescriptionFactory = _ => "Set the maximum number of downloads that can happen in parallel when downloading collections",
            },
            SingleValueMultipleChoiceContainerOptions.Create<int>(
                valueComparer: EqualityComparer<int>.Default,
                allowedValues: Enumerable.Range(start: 1, Math.Max(Environment.ProcessorCount, 16)).ToArray(),
                valueToDisplayString: static i => i.ToString()
            )
        );
    }
}
