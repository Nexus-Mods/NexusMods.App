using NexusMods.Abstractions.GameLocators;
using NexusMods.HyperDuck;

namespace NexusMods.DataModel.Synchronizer.DbFunctions;

/// <summary>
/// 
/// </summary>
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
        var strsVector = chunk.GetVector(0);
        var strsElements = strsVector.GetData<StringElement>();
        var mask = strsVector.GetValidityMask();
        
        var output = vector.GetData<ushort>();
        var outputMask = vector.GetValidityMask();

        for (ulong row = 0; row < (ulong)strsElements.Length; row++)
        {
            if (!mask.IsValid(row))
            {
                outputMask[row] = false;
                continue;
            }

            var hash = FNV1aHash.MixToShort(FNV1aHash.Hash(strsElements[(int)row].GetSpan()));
            output[(int)row] = hash;
            outputMask[row] = true;
        }
    }
}
