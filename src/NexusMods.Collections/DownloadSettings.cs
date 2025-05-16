using NexusMods.Abstractions.Settings;

namespace NexusMods.Collections;

public class DownloadSettings : ISettings
{
    public int MaxParallelDownloads { get; set; } = Environment.ProcessorCount;

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.AddToUI<DownloadSettings>(builder => builder
            .AddPropertyToUI(x => x.MaxParallelDownloads, propertyBuilder => propertyBuilder
                .AddToSection(Sections.General)
                .WithDisplayName("Max Parallel Downloads")
                .WithDescription("Set the maximum number of downloads that can happen in parallel when downloading collections")
                .UseSingleValueMultipleChoiceContainer(
                    valueComparer: EqualityComparer<int>.Default,
                    allowedValues: Enumerable.Range(start: 1, Environment.ProcessorCount).ToArray(),
                    valueToDisplayString: static i => i.ToString()
                )
            )
        );
    }
}
