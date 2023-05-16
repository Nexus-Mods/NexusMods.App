using System.Xml.Serialization;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.Games.DarkestDungeon.Models;

/// <summary>
/// Represents the data structure of a <c>project.xml</c> file.
/// </summary>
[JsonName("NexusMods.Games.DarkestDungeon.ModProject")]
[XmlRoot(ElementName = "project")]
public record ModProject : IFileAnalysisData
{
    [XmlElement(ElementName = "Title")]
    public string Title { get; set; } = string.Empty;

    [XmlElement(ElementName = "Language")]
    public string Language { get; set; } = "english";

    [XmlElement(ElementName = "ItemDescription")]
    public string ItemDescription { get; set; } = string.Empty;

    [XmlElement(ElementName = "PreviewIconFile")]
    public string PreviewIconFile { get; set; } = string.Empty;

    [XmlElement(ElementName = "VersionMajor")]
    public int VersionMajor { get; set; }

    [XmlElement(ElementName = "VersionMinor")]
    public int VersionMinor { get; set; }

    [XmlElement(ElementName = "ModDataPath")]
    public string ModDataPath { get; set; } = string.Empty;

    [XmlElement(ElementName = "PublishedFileId")]
    public int PublishedFileId { get; set; }

    [XmlElement(ElementName = "Visibility")]
    public string Visibility { get; set; } = "private";

    [XmlElement(ElementName = "UploadMode")]
    public string UploadMode { get; set; } = "dont_submit";

    [XmlElement(ElementName = "ItemDescriptionShort")]
    public string ItemDescriptionShort { get; set; } = string.Empty;

    [XmlElement(ElementName = "UpdateDetails")]
    public string UpdateDetails { get; set; } = string.Empty;
}
