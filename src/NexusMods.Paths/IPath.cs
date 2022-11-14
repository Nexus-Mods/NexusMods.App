namespace NexusMods.Paths;

public interface IPath
{
    Extension Extension { get; }
    RelativePath FileName { get; }
}