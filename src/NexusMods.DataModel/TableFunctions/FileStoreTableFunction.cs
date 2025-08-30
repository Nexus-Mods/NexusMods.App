using System.Collections.Immutable;
using NexusMods.Hashing.xxHash3;
using NexusMods.HyperDuck;

namespace NexusMods.DataModel.TableFunctions;

public class FileStoreTableFunction : ATableFunction
{
    private readonly NxFileStore _fileStore;

    public FileStoreTableFunction(NxFileStore fileStore)
    {
        _fileStore = fileStore;
    }

    protected override void Setup(RegistrationInfo info)
    {
        info.SetName("nma_archivedfiles");
    }

    protected override void Execute(FunctionInfo functionInfo)
    {
        var chunk = functionInfo.Chunk;
        var column = functionInfo.GetWritableVector(0);
        var initData = functionInfo.GetInitInfo<LocalInitData>();

        var block = initData.GetNextBlock();
        if (block.IsEmpty)
        {
            chunk.Size = 0;
            return;
        }
        
        block.CopyTo(column.GetData<Hash>());
        chunk.Size = (ulong)block.Length;
    }

    protected override void Bind(BindInfo info)
    {
        info.AddColumn<ulong>("Hash");
    }

    protected override object? Init(InitInfo initInfo, InitData initData)
    {
        var keys = _fileStore.AllHashes;
        initInfo.SetMaxThreads(keys.Length / (int)GlobalConstants.DefaultVectorSize);
        return new LocalInitData()
        {
            Hashes = keys
        };
    }

    private class LocalInitData
    {
        public ImmutableArray<Hash> Hashes { get; init; }
        private int _block = -1;

        public ReadOnlySpan<Hash> GetNextBlock()
        {
            var block = Interlocked.Increment(ref _block);
            var startAt = block * (int)GlobalConstants.DefaultVectorSize;
            if (startAt >= Hashes.Length) 
                return ReadOnlySpan<Hash>.Empty;
            var length = Math.Min(Hashes.Length - startAt, (int)GlobalConstants.DefaultVectorSize);
            return Hashes.AsSpan().Slice(startAt, length);
        }
    }
}
