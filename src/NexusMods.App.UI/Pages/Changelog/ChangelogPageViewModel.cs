using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Reloaded.Memory.Extensions;

namespace NexusMods.App.UI.Pages.Changelog;

[UsedImplicitly]
public class ChangelogPageViewModel : APageViewModel<IChangelogPageViewModel>, IChangelogPageViewModel
{
    private readonly Uri _changelogUri = new("https://raw.githubusercontent.com/Nexus-Mods/NexusMods.App/main/CHANGELOG.md");

    public IMarkdownRendererViewModel MarkdownRendererViewModel { get; }

    [Reactive] public Version? TargetVersion { get; set; }
    [Reactive] public ParsedChangelog? ParsedChangelog { get; set; }
    [Reactive] public int SelectedIndex { get; set; }

    private string? _fullChangelog;

    public ChangelogPageViewModel(
        IServiceProvider serviceProvider,
        IWindowManager windowManager) : base(windowManager)
    {
        TabIcon = IconValues.FileDocumentOutline;
        TabTitle = Language.ChangelogPageViewModel_ChangelogPageViewModel_Changelog;

        MarkdownRendererViewModel = serviceProvider.GetRequiredService<IMarkdownRendererViewModel>();

        this.WhenActivated(disposables =>
        {
            MarkdownRendererViewModel
                .WhenAnyValue(vm => vm.Contents)
                .Where(contents => !string.IsNullOrWhiteSpace(contents))
                .Where(_ => _fullChangelog is null)
                .SubscribeWithErrorLogging(contents =>
                {
                    _fullChangelog = contents;
                    ParsedChangelog = ParsedChangelog.Parse(contents);
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(
                vm => vm.ParsedChangelog,
                vm => vm.TargetVersion)
                .Where(_ => _fullChangelog is not null)
                .SubscribeWithErrorLogging(tuple =>
                {
                    var (parsedChangelog, targetVersion) = tuple;
                    if (parsedChangelog is null) return;

                    string? contents = null;
                    if (targetVersion is not null)
                    {
                        contents = parsedChangelog.GetVersionSection(targetVersion);
                    }

                    MarkdownRendererViewModel.Contents = contents ?? _fullChangelog!;

                    var index = targetVersion is null ? 0 : parsedChangelog.Versions
                        .Select(kv => kv.Key)
                        .IndexOf(targetVersion) + 1;
                    SelectedIndex = index;
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.SelectedIndex)
                .Where(_ => ParsedChangelog is not null)
                .Select(index =>
                {
                    // "ALL" is at index 0
                    var actualIndex = index - 1;
                    if (actualIndex < 0) return null;

                    return ParsedChangelog!.Versions[actualIndex].Key;
                })
                .BindToVM(this, vm => vm.TargetVersion)
                .DisposeWith(disposables);

            MarkdownRendererViewModel.MarkdownUri = _changelogUri;
        });
    }
}

public record ParsedChangelog
{
    public required string Changelog { get; init; }

    public required int[] SectionIndices { get; init; }

    public required KeyValuePair<Version, int>[] Versions { get; init; }

    private Range GetSectionRange(int startIndex)
    {
        var end = -1;

        foreach (var index in SectionIndices)
        {
            if (index <= startIndex) continue;
            end = index;
            break;
        }

        if (end == -1) end = Changelog.Length;
        return new Range(startIndex, end);
    }

    public string? GetVersionSection(Version version)
    {
        if (version.Revision is 0 or -1)
        {
            version = new Version(
                version.Major,
                version.Minor,
                version.Build
            );
        }
        else
        {
            version = new Version(
                version.Major,
                version.Minor,
                version.Build,
                version.Revision
            );
        }

        foreach (var kv in Versions)
        {
            var (other, lineIndex) = kv;
            if (!other.Equals(version)) continue;

            var range = GetSectionRange(lineIndex);
            var (offset, length) = range.GetOffsetAndLength(Changelog.Length);
            return Changelog.Substring(startIndex: offset, length);
        }

        return null;
    }

    public static ParsedChangelog Parse(string changelog)
    {
        var span = changelog.AsSpan();

        const string linePrefix = "# ";
        const string versionPrefix = "v";

        var sectionIndices = new List<int>();
        var versions = new List<KeyValuePair<Version, int>>();

        var remaining = span;
        var lineIndex = 0;

        while (true)
        {
            var newLineIndex = remaining.IndexOf('\n');
            if (newLineIndex == -1 || newLineIndex == remaining.Length - 1) break;

            var currentLineIndex = lineIndex;
            lineIndex += newLineIndex + 1;

            var currentLine = remaining.SliceFast(start: 0, length: newLineIndex);
            remaining = remaining.SliceFast(start: newLineIndex + 1);

            if (!currentLine.StartsWith(linePrefix) || currentLine.Length < linePrefix.Length) continue;
            sectionIndices.Add(currentLineIndex);

            var slice = currentLine.Slice(start: linePrefix.Length);

            if (!slice.StartsWith(versionPrefix) || slice.Length < versionPrefix.Length) continue;
            slice = slice.Slice(start: versionPrefix.Length);

            var index = slice.IndexOf(' ');
            if (index == -1) continue;

            slice = slice.Slice(start: 0, length: index);
            var version = new Version(slice.ToString());

            versions.Add(new KeyValuePair<Version, int>(version, currentLineIndex));
        }

        return new ParsedChangelog
        {
            Changelog = changelog,
            SectionIndices = sectionIndices.ToArray(),
            Versions = versions.ToArray(),
        };
    }
}
