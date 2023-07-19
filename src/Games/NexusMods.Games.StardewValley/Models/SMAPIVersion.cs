using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.Extensions.Primitives;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.StardewValley.Models;

// ReSharper disable InconsistentNaming

/// <summary>
/// https://github.com/Pathoschild/SMAPI/blob/3dc45bb85ed4399edfe170fb26aa4ddb2f53d66c/src/SMAPI.Toolkit.CoreInterfaces/ISemanticVersion.cs#L7
/// https://github.com/Pathoschild/SMAPI/blob/3dc45bb85ed4399edfe170fb26aa4ddb2f53d66c/src/SMAPI.Toolkit/SemanticVersion.cs#L17
/// </summary>
[PublicAPI]
[JsonConverter(typeof(SMAPIVersionConverter))]
public sealed record SMAPIVersion
{
    public required int MajorVersion { get; init; }

    public required int MinorVersion { get; init; }

    public int PatchVersion { get; init; }

    public int PlatformRelease { get; init; }

    public string? PrereleaseTag { get; init; }

    public string? BuildMetadata { get; init; }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append(MajorVersion);
        sb.Append('.');
        sb.Append(MinorVersion);

        if (PatchVersion != 0)
        {
            sb.Append('.');
            sb.Append(PatchVersion);
        }

        if (PrereleaseTag is not null)
        {
            sb.Append('-');
            sb.Append(PrereleaseTag);
        }

        if (BuildMetadata is not null)
        {
            sb.Append('+');
            sb.Append(BuildMetadata);
        }

        return sb.ToString();
    }

    public static SMAPIVersion From(Version version)
    {
        return new SMAPIVersion
        {
            MajorVersion = version.Major,
            MinorVersion = version.Minor,
            PatchVersion = version.Build == -1 ? 0 : version.Build
        };
    }

    public static bool TryParse(ReadOnlySpan<char> input, [NotNullWhen(true)] out SMAPIVersion? version)
    {
        if (!TryParseParts(
                input,
                out var majorVersion,
                out var minorVersion,
                out var patchVersion,
                out var platformRelease,
                out var prereleaseTag,
                out var buildMetadata))
        {
            version = null;
            return false;
        }

        version = new SMAPIVersion
        {
            MajorVersion = majorVersion,
            MinorVersion = minorVersion,
            PatchVersion = patchVersion,
            PlatformRelease = platformRelease,
            PrereleaseTag = prereleaseTag,
            BuildMetadata = buildMetadata
        };

        return true;
    }

    private static bool TryParseParts(
        ReadOnlySpan<char> input,
        out int majorVersion,
        out int minorVersion,
        out int patchVersion,
        out int platformRelease,
        out string? prereleaseTag,
        out string? buildMetadata)
    {
        majorVersion = 0;
        minorVersion = 0;
        patchVersion = 0;
        platformRelease = 0;
        prereleaseTag = null;
        buildMetadata = null;

        var i = 0;

        // complete example: "12.34.56-beta434+debug93"

        // required major and minor version
        if (!TryParseVersionPart(input, ref i, out majorVersion)) return false;
        if (!TryParseLiteral(input, ref i, '.')) return false;
        if (!TryParseVersionPart(input, ref i, out minorVersion)) return false;

        // optional patch version
        if (TryParseLiteral(input, ref i, '.') && !TryParseVersionPart(input, ref i, out patchVersion))
            return false;

        // // optional non-standard platform release version
        // if (TryParseLiteral(input, ref i, '.') && !TryParseVersionPart(input, ref i, out platformRelease))
        //     return false;

        // optional prerelease tag
        if (TryParseLiteral(input, ref i, '-') && !TryParseTag(input, ref i, out prereleaseTag))
            return false;

        // optional build tag
        if (TryParseLiteral(input, ref i, '+') && !TryParseTag(input, ref i, out buildMetadata))
            return false;

        return i == input.Length;
    }

    private static bool TryParseVersionPart(ReadOnlySpan<char> input, ref int index, out int part)
    {
        part = 0;
        var start = index;

        int i;
        for (i = index; i < input.Length && char.IsDigit(input[i]); i++) { }

        if (i == 0) return false;
        var slice = input.SliceFast(start, i - start);

        // no leading zeroes
        if (slice.Length > 1 && slice[0] == '0') return false;

        index += slice.Length;
        return int.TryParse(slice, CultureInfo.InvariantCulture, out part);
    }

    private static bool TryParseLiteral(ReadOnlySpan<char> input, ref int index, char literal)
    {
        if (index >= input.Length || input[index] != literal) return false;
        index++;
        return true;
    }

    private static bool TryParseTag(ReadOnlySpan<char> input, ref int index, [NotNullWhen(true)] out string? tag)
    {
        var length = 0;
        for (var i = index; i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == '-' || input[i] == '.'); i++)
        {
            length++;
        }

        if (length == 0)
        {
            tag = null;
            return false;
        }

        tag = input.SliceFast(index, length).ToString();
        index += length;
        return true;
    }
}

public class SMAPIVersionConverter : JsonConverter<SMAPIVersion>
{
    public override SMAPIVersion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected {JsonTokenType.String}, found {reader.TokenType}");

        var s = reader.GetString();

        if (s is null) return null;

        if (SMAPIVersion.TryParse(s, out var version)) return version;
        throw new JsonException($"Unable to parse \"{s}\"!");
    }

    public override void Write(Utf8JsonWriter writer, SMAPIVersion value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
