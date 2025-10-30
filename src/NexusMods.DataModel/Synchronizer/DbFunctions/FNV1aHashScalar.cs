using NexusMods.HyperDuck;
using NexusMods.Sdk.Hashes;

namespace NexusMods.DataModel.Synchronizer.DbFunctions;

public class FNV1aHashScalar : AScalarFunction
{
    public override void Setup()
    {
        SetName("nma_fnv1a_hash_short");
        AddParameter<string>();
        SetReturnType<ushort>();
    }

    public override void Execute(ReadOnlyChunk chunk, WritableVector vector)
    {
        var readVector = chunk.GetVector(0);
        var elements = readVector.GetData<StringElement>();
        var mask = readVector.GetValidityMask();

        var output = vector.GetData<ushort>();
        var outputMask = vector.GetValidityMask();

        for (ulong row = 0; row < (ulong)elements.Length; row++)
        {
            if (!mask.IsValid(row))
            {
                outputMask[row] = false;
                continue;
            }

            var element = elements[(int)row];
            var hash = FNV1a16Hasher.Hash(element.GetSpan());
            output[(int)row] = hash;
            outputMask[row] = true;
        }
    }
}
