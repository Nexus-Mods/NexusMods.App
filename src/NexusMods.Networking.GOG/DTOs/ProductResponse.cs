using System.Text.Json.Serialization;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedType.Global
// ReSharper disable NotAccessedPositionalProperty.Global

namespace NexusMods.Networking.GOG.DTOs;

public record ContentSystemCompatibility(
    [property: JsonPropertyName("windows")] bool Windows,
    [property: JsonPropertyName("osx")] bool OSX,
    [property: JsonPropertyName("linux")] bool Linux
);

public record Dlcs(
    [property: JsonPropertyName("products")] IReadOnlyList<Product> Products,
    [property: JsonPropertyName("all_products_url")] string AllProductsUrl,
    [property: JsonPropertyName("expanded_all_products_url")] string ExpandedAllProductsUrl
);

public record Downloads(
    [property: JsonPropertyName("installers")] IReadOnlyList<Installer> Installers,
    [property: JsonPropertyName("patches")] IReadOnlyList<Patch> Patches
);

public record File(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("size")] int Size,
    [property: JsonPropertyName("downlink")] string Downlink
);

public record Installer(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("os")] string OS,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("language_full")] string LanguageFull,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("total_size")] int TotalSize,
    [property: JsonPropertyName("files")] IReadOnlyList<File> Files
);

public record Patch(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("os")] string OS,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("language_full")] string LanguageFull,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("total_size")] int TotalSize,
    [property: JsonPropertyName("files")] IReadOnlyList<File> Files
);

public record Product(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("link")] string Link,
    [property: JsonPropertyName("expanded_link")] string ExpandedLink
);

public record ProductResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("purchase_link")] string PurchaseLink,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("content_system_compatibility")] ContentSystemCompatibility ContentSystemCompatibility,
    [property: JsonPropertyName("dlcs")] Dlcs Dlcs,
    [property: JsonPropertyName("downloads")] Downloads Downloads
);

