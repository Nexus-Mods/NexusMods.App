using System.IO.MemoryMappedFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Interprocess;

/// <summary>
/// A shared array of 64-bit unsigned integers, supports atomic operations via
/// a CAS operation on a mmaped file.
/// </summary>
public unsafe class SharedArray : IDisposable
{
    private readonly int _totalSize;
    private Stream _stream;
    private MemoryMappedFile _mmapFile;
    private MemoryMappedViewAccessor _view;
    private readonly string _name;

    public SharedArray(AbsolutePath path, int itemCount)
    {
        _totalSize = itemCount * sizeof(ulong);

        while (true)
        {
            try
            {
                // Make sure the a previous process didn't exit without setting the file size
                if (path.FileExists)
                {
                    var tmpStream =
                        path.Open(FileMode.Open, FileAccess.ReadWrite);
                    tmpStream.Position = _totalSize;
                    tmpStream.Close();
                }
                else
                {
                    var tmpStream = path.Open(FileMode.CreateNew,
                        FileAccess.ReadWrite);
                    tmpStream.Position = _totalSize;
                    tmpStream.Close();
                }
                break;
            }
            catch (IOException)
            {
                continue;
            }
        }

        _name = path.GetFullPath().ToLower().XxHash64AsUtf8().ToHex();
        _stream = path.Open(FileMode.Open, FileAccess.ReadWrite);

        #if !WINDOWS
        // On non-Windows platforms we can use the standard MemoryMappedFile API
        _mmapFile = MemoryMappedFile.CreateFromFile((FileStream)_stream,
            null,
            _totalSize,
            MemoryMappedFileAccess.ReadWrite,
            HandleInheritability.Inheritable,
            false);
        #else
        // On Windows we have to use this Windows-only API because Win32 freaks out
        // if you try to open a file that's already open.
        _mmapFile = MemoryMappedFile.CreateOrOpen(_name, _totalSize);
        #endif
        _view = _mmapFile.CreateViewAccessor(0, _totalSize, MemoryMappedFileAccess.ReadWrite);
    }

    /// <summary>
    /// Get the value at the given index
    /// </summary>
    /// <param name="idx"></param>
    /// <returns></returns>
    public ulong Get(int idx)
    {
#if DEBUG
        if (idx < 0 || idx >= _totalSize / sizeof(ulong))
        {
            throw new ArgumentOutOfRangeException(nameof(idx));
        }
#endif
        _view.Read(idx * sizeof(ulong), out ulong value);
        return value;
    }

    /// <summary>
    /// Set the value at the given index to the given value if the current value is the expected value.
    /// Returns true if the value was set, false otherwise.
    /// </summary>
    /// <param name="idx"></param>
    /// <param name="expected"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool CompareAndSwap(int idx, ulong expected, ulong value)
    {
#if DEBUG
        if (idx < 0 || idx >= _totalSize / sizeof(ulong))
        {
            throw new ArgumentOutOfRangeException(nameof(idx));
        }
#endif

        try
        {
            byte* ptr = null;
            _view.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            var result = Interlocked.CompareExchange(ref *(ulong*)(ptr + idx * sizeof(ulong)), value, expected);
            return result == expected;
        }
        finally
        {
            _view.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _view.Dispose();
        _mmapFile.Dispose();
        _stream.Dispose();
    }
}
