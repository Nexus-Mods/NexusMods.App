using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NexusMods.Abstractions.NexusWebApi.DTOs.Interfaces;

// 👇 Suppress uninitialised variables. Currently Nexus has mostly read-only API and we expect server to return the data.
#pragma warning disable CS8618

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace NexusMods.Abstractions.NexusWebApi.DTOs;

/// <summary>
/// Represents the collection of downloadable files tied to a mod.
/// </summary>
public class ModFiles : IJsonSerializable<ModFiles>
{
    /// <summary/>
    [JsonPropertyName("files")]
    public ModFile[] Files { get; set; }

    /// <inheritdoc />
    public static JsonTypeInfo<ModFiles> GetTypeInfo() => ModFilesContext.Default.ModFiles;
}

/// <summary/>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ModFiles))]
public partial class ModFilesContext : JsonSerializerContext { }
