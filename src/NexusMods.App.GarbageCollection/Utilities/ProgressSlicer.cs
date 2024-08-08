using System.Diagnostics.CodeAnalysis;
namespace NexusMods.App.GarbageCollection.Utilities;

/// <summary>
///     A class that splits up the reporting of progress into multiple "slices"; such that
///     multiple operations can be reported using one progress.
/// </summary>
[ExcludeFromCodeCoverage] // This is copied from Sewer56.Update.Misc (https://github.com/Sewer56/Update/blob/9c6c94821120468bb7d0fb3cb7dad03800c29f50/Sewer56.Update/Sewer56.Update.Misc/ProgressSlicer.cs)
public class ProgressSlicer
{
    private readonly IProgress<double>? _output;
    private readonly Dictionary<int, double> _splitTotals;

    private int _splitCount;

    /// <summary/>
    /// <param name="output">The progress instance to output the progress to.</param>
    public ProgressSlicer(IProgress<double>? output)
    {
        _output = output;
        _splitTotals = new Dictionary<int, double>();
    }

    /// <summary>
    /// Creates a slice, allowing to report part of the full (1.0)
    /// progress to the configured output as a 0.0 to 1.0 value.
    /// </summary>
    /// <param name="multiplier">
    ///     The amount of percent this slice is worth.
    ///     A value between 0.0 and 1.0, with 1.0 representing 100% of the progress.
    /// </param>
    public IProgress<double> Slice(double multiplier)
    {
        var index = _splitCount++;
        return new Progress<double>(p =>
        {
            lock (_splitTotals)
            {
                _splitTotals[index] = multiplier * p;

                var sum = 0.0;
                foreach (var value in _splitTotals.Values)
                    sum += value;

                _output?.Report(sum);
            }
        });
    }
}
