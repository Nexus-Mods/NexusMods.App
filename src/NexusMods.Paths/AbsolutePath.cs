using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace NexusMods.Paths;

/// <summary>
/// Flags if the path should use Unix or Windows path separators.
/// </summary>
public enum PathFormat : byte
{
    Windows = 0,
    Unix
}

/// <summary>
/// A path that represents a full path to a file or directory.
/// </summary>
public struct AbsolutePath : IPath, IComparable<AbsolutePath>, IEquatable<AbsolutePath>
{
    public static readonly AbsolutePath Empty = "".ToAbsolutePath();
    public PathFormat PathFormat { get; }

    private int _hashCode = 0;

    public readonly string[] Parts = Array.Empty<string>();

    public Extension Extension => Extension.FromPath(Parts[^1]);
    public RelativePath FileName => new(Parts[^1..]);

    internal AbsolutePath(string[] parts, PathFormat format)
    {
        Parts = parts;
        PathFormat = format;
    }

    internal static readonly char[] StringSplits = { '/', '\\' };

    private static AbsolutePath Parse(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return default;
        var parts = path.Split(StringSplits, StringSplitOptions.RemoveEmptyEntries);
        return new AbsolutePath(parts, DetectPathType(path));
    }

    private static readonly HashSet<char>
        DriveLetters = new("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

    private static PathFormat DetectPathType(string path)
    {
        if (path.StartsWith("/"))
            return PathFormat.Unix;
        if (path.StartsWith(@"\\"))
            return PathFormat.Windows;

        if (path.Length >= 2 && DriveLetters.Contains(path[0]) && path[1] == ':')
            return PathFormat.Windows;

        throw new PathException($"Invalid Path format: {path}");
    }

    public AbsolutePath Parent
    {
        get
        {
            {
                if (Parts.Length <= 1)
                    throw new PathException($"Path {this} does not have a parent folder");
                var newParts = new string[Parts.Length - 1];
                Array.Copy(Parts, newParts, newParts.Length);
                return new AbsolutePath(newParts, PathFormat);
            }
        }
    }

    public int Depth => Parts?.Length ?? 0;

    /// <summary>
    /// Returns a IEnumerable of this path and all 
    /// </summary>
    /// <returns></returns>
    public IEnumerable<AbsolutePath> ThisAndAllParents()
    {
        var p = this;
        while (true)
        {
            yield return p;
            if (p.Depth == 1)
                yield break;
            p = p.Parent;
        }
    }

    /// <summary>
    /// Returns a new path that is this path with the extension changed.
    /// </summary>
    /// <param name="newExtension"></param>
    /// <returns></returns>
    public readonly AbsolutePath ReplaceExtension(Extension newExtension)
    {
        var paths = new string[Parts.Length];
        Array.Copy(Parts, paths, paths.Length);
        var oldName = paths[^1];
        var newName = RelativePath.ReplaceExtension(oldName, newExtension);
        paths[^1] = newName;
        return new AbsolutePath(paths, PathFormat);
    }

    public static explicit operator AbsolutePath(string input)
    {
        return Parse(input);
    }

    public readonly override string ToString()
    {
        if (Parts == default) return "";
        if (PathFormat != PathFormat.Windows) 
            return "/" + string.Join('/', Parts);
        return Parts.Length == 1 ? $"{Parts[0]}\\" : string.Join('\\', Parts);
    }

    public override int GetHashCode()
    {
        if (_hashCode != 0) return _hashCode;
        if (Parts == null || Parts.Length == 0) return -1;

        var result = 0;
        foreach (var part in Parts)
            result ^= part.GetHashCode(StringComparison.CurrentCultureIgnoreCase);
        _hashCode = result;
        return _hashCode;
    }

    public override bool Equals(object? obj)
    {
        return obj is AbsolutePath path && Equals(path);
    }

    public int CompareTo(AbsolutePath other)
    {
        return ArrayExtensions.CompareString(Parts, other.Parts);
    }

    public bool Equals(AbsolutePath other)
    {
        if (other.Depth != Depth) return false;
        for (var idx = 0; idx < Parts.Length; idx++)
            if (!Parts[idx].Equals(other.Parts[idx], StringComparison.InvariantCultureIgnoreCase))
                return false;
        return true;
    }

    public RelativePath RelativeTo(AbsolutePath basePath)
    {
        if (!ArrayExtensions.AreEqualIgnoreCase(basePath.Parts, 0, Parts, 0, basePath.Parts.Length))
            throw new PathException($"{basePath} is not a base path of {this}");

        var newParts = new string[Parts.Length - basePath.Parts.Length];
        Array.Copy(Parts, basePath.Parts.Length, newParts, 0, newParts.Length);
        return new RelativePath(newParts);
    }

    /// <summary>
    /// Returns true if this path is a child of the given path.
    /// </summary>
    /// <param name="parent"></param>
    /// <returns></returns>
    public bool InFolder(AbsolutePath parent)
    {
        return ArrayExtensions.AreEqualIgnoreCase(parent.Parts, 0, Parts, 0, parent.Parts.Length);
    }

    /// <summary>
    /// Combines this path with the given relative path(s).
    /// </summary>
    /// <param name="paths"></param>
    /// <returns></returns>
    /// <exception cref="PathException"></exception>
    public readonly AbsolutePath Join(params object[] paths)
    {
        var converted = paths.Select(p =>
        {
            return p switch
            {
                string s => (RelativePath)s,
                RelativePath path => path,
                _ => throw new PathException($"Cannot cast {p} of type {p.GetType()} to Path")
            };
        }).ToArray();
        return Join(converted);
    }

    /// <summary>
    /// Combines 
    /// </summary>
    /// <param name="paths"></param>
    /// <returns></returns>
    public readonly AbsolutePath Join(params RelativePath[] paths)
    {
        var newLen = Parts.Length + paths.Sum(p => p.Parts.Length);
        var newParts = new string[newLen];
        Array.Copy(Parts, newParts, Parts.Length);

        var toIdx = Parts.Length;
        foreach (var p in paths)
        {
            Array.Copy(p.Parts, 0, newParts, toIdx, p.Parts.Length);
            toIdx += p.Parts.Length;
        }

        return new AbsolutePath(newParts, PathFormat);
    }

    public static bool operator ==(AbsolutePath a, AbsolutePath b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(AbsolutePath a, AbsolutePath b)
    {
        return !a.Equals(b);
    }

    public AbsolutePath WithExtension(Extension ext)
    {
        var parts = new string[Parts.Length];
        Array.Copy(Parts, parts, Parts.Length);
        parts[^1] += ext;
        return new AbsolutePath(parts, PathFormat);
    }

    public AbsolutePath AppendToName(string append)
    {
        return Parent.Join((FileName.FileNameWithoutExtension + append).ToRelativePath()
            .WithExtension(Extension));
    }

    #region IO

    public const int BufferSize = 1024 * 128;

    public Stream Open(FileMode mode, FileAccess access = FileAccess.Read,
        FileShare share = FileShare.ReadWrite)
    {
        return File.Open(ToNativePath(), mode, access, share);
    }

    public Stream Read()
    {
        return Open(FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public Stream Create()
    {
        return Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);
    }

    public void Delete()
    {
        if (FileExists)
        {
            try
            {
                File.Delete(ToNativePath());
            }
            catch (UnauthorizedAccessException)
            {
                var fi = FileInfo;
                if (fi.IsReadOnly)
                {
                    fi.IsReadOnly = false;
                    File.Delete(ToNativePath());
                }
                else
                {
                    throw;
                }
            }
        }
        if (Directory.Exists(ToNativePath()))
            DeleteDirectory();
    }

    private FileInfo? _info = null;
    public FileInfo FileInfo => _info ??= new FileInfo(ToString());
    
    public FileVersionInfo VersionInfo => FileVersionInfo.GetVersionInfo(ToNativePath());
    
    public Size Length => Size.From(FileInfo.Length);

    public DateTime LastWriteTimeUtc => FileInfo.LastWriteTimeUtc;
    public DateTime CreationTimeUtc => FileInfo.CreationTimeUtc;
    public DateTime LastWriteTime => FileInfo.LastWriteTime;
    public DateTime CreationTime => FileInfo.CreationTime;
    
    public async Task<byte[]> ReadAllBytesAsync(CancellationToken? token = null)
    {
        await using var s = Read();
        var remain = s.Length;
        var length = remain;
        var bytes = new byte[length];

        while (remain > 0)
        {
            var offset = (int)Math.Min(length - remain, 1024 * 1024);
            remain -= await s.ReadAsync(bytes, offset, (int)remain, token ?? CancellationToken.None);
        }

        return bytes;
    }
    
    public async ValueTask MoveToAsync(AbsolutePath dest, bool overwrite = true,
        CancellationToken? token = null)
    {
        var srcStr = ToNativePath();
        var destStr = dest.ToString();
        var fi = new FileInfo(srcStr);
        if (fi.IsReadOnly)
            fi.IsReadOnly = false;

        var fid = new FileInfo(destStr);
        if (dest.FileExists && fid.IsReadOnly)
        {
            fid.IsReadOnly = false;
        }

        var retries = 0;
        while (true)
        {
            try
            {
                File.Move(srcStr, destStr, overwrite);
                return;
            }
            catch (Exception)
            {
                if (retries > 10)
                    throw;
                retries++;
                await Task.Delay(TimeSpan.FromSeconds(1), token ?? CancellationToken.None);
            }
        }
    }

    public async ValueTask CopyToAsync(AbsolutePath dest, CancellationToken? token = null)
    {
        await using var inf = Read();
        await using var ouf = dest.Create();
        await inf.CopyToAsync(ouf, token ?? CancellationToken.None);
    }
    
    public async ValueTask CopyFromAsync(Stream src, CancellationToken token = default)
    {
        await using var output = Create();
        await src.CopyToAsync(output, token);
    }
    
    private readonly string ToNativePath()
    {
        return ToString();
    }

    public void CreateDirectory()
    {
        if (Depth > 1 && !Parent.DirectoryExists())
            Parent.CreateDirectory();
        Directory.CreateDirectory(ToNativePath());
    }

    public readonly void DeleteDirectory(bool dontDeleteIfNotEmpty = false)
    {
        if (!DirectoryExists()) return;
        if (dontDeleteIfNotEmpty && (EnumerateFiles().Any() || EnumerateDirectories().Any())) return;
      
        foreach (var directory in Directory.GetDirectories(ToString()))
        {
            directory.ToAbsolutePath().DeleteDirectory(dontDeleteIfNotEmpty);
        }
        try
        {
            var di = new DirectoryInfo(ToNativePath());
            if (di.Attributes.HasFlag(FileAttributes.ReadOnly))
                di.Attributes &= ~FileAttributes.ReadOnly;
            Directory.Delete(ToString(), true);
        }
        catch (UnauthorizedAccessException)
        {
            Directory.Delete(ToString(), true);
        }
    }

    public readonly bool DirectoryExists()
    {
        return Parts.Length != 0 && Directory.Exists(ToNativePath());
    }

    public bool FileExists => Parts.Length != 0 && File.Exists(ToNativePath());
    public AbsolutePath TopParent => PathFormat == PathFormat.Windows ? 
        new(Parts[..1], PathFormat) : 
        new AbsolutePath(Array.Empty<string>(), PathFormat);

    public readonly IEnumerable<AbsolutePath> EnumerateFiles(string pattern = "*",
        bool recursive = true)
    {
        return Directory.EnumerateFiles(ToNativePath(), pattern,
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Select(file => file.ToAbsolutePath());
    }
    
    public IEnumerable<FileEntry> EnumerateFileEntries(string pattern = "*",
        bool recursive = true)
    {
        if (!DirectoryExists()) return Array.Empty<FileEntry>();
        return Directory.EnumerateFiles(ToNativePath(), pattern,
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Select(file =>
            {
                var path = file.ToAbsolutePath();
                var info = path.FileInfo;
                return new FileEntry(Path: path, Size: Size.From(info.Length), LastModified:info.LastWriteTimeUtc);
            });
    }


    public IEnumerable<AbsolutePath> EnumerateFiles(Extension pattern,
        bool recursive = true)
    {
        return Directory.EnumerateFiles(ToString(), "*" + pattern,
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Select(file => file.ToAbsolutePath());
    }


    public readonly IEnumerable<AbsolutePath> EnumerateDirectories(bool recursive = true)
    {
        if (!DirectoryExists()) return Array.Empty<AbsolutePath>();
        return Directory.EnumerateDirectories(ToNativePath(), "*",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Select(p => (AbsolutePath) p);
    }

    #endregion

    public async Task WriteAllTextAsync(string text, CancellationToken? token = null)
    {
        await using var fs = Create();
        await fs.WriteAsync(Encoding.UTF8.GetBytes(text), token ?? CancellationToken.None);
    }

    public async Task WriteAllLinesAsync(IEnumerable<string> lines, CancellationToken token = default)
    {
        await using var fs = Create();
        await using var sw = new StreamWriter(fs);
        foreach (var line in lines)
        {
            await sw.WriteLineAsync(line.AsMemory(), token);
        }
    }
    
    public async Task<string> ReadAllTextAsync(CancellationToken? token = null)
    {
        return Encoding.UTF8.GetString(await ReadAllBytesAsync(token));
    }

    public async Task WriteAllBytesAsync(byte[] emptyArray)
    {
        await using var fs = Create();
        await fs.WriteAsync(emptyArray, CancellationToken.None);
    }
}