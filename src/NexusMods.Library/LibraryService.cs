using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Library;

/// <summary>
/// Implementation of <see cref="ILibraryService"/>.
/// </summary>
public sealed class LibraryService : ILibraryService
{
    private readonly ILogger _logger;
    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Constructor.
    /// </summary>
    public LibraryService(ILogger<LibraryService> logger, IConnection connection, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _connection = connection;
        _serviceProvider = serviceProvider;
    }

    public IJob AddLocalFile(AbsolutePath absolutePath)
    {
        var group = new AddLocalFileJobGroup(worker: _serviceProvider.GetRequiredService<AddLocalFileJobGroupWorker>())
        {
            Transaction = _connection.BeginTransaction(),
            FilePath = absolutePath,
        };

        return group;
    }
}
