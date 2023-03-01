using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusMods.Common;

/// <summary>
/// abstraction for functions generating unique ids
/// </summary>
public interface IIDGenerator
{
    /// <summary>
    /// generate a UUIDv4 <see href="https://datatracker.ietf.org/doc/html/rfc4122#section-4.4"/>
    /// </summary>
    string UUIDv4();
}
