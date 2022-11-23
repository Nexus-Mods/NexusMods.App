using System.Text.Json;
using NexusMods.Paths;
using PropertyChanged;

namespace NexusMods.DataModel;

[AddINotifyPropertyChangedInterface]
public class ModFile : AVersionedObject
{
    public GamePath To { get; set; }
}