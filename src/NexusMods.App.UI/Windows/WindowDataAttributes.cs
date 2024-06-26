
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.App.UI.Windows;

public partial class WindowDataAttributes : IModelDefinition
{
    private const string Namespace = "NexusMods.App.UI.Windows.WindowData";
    
    /// <summary>
    /// The state of the window, stored as json
    /// </summary>
    public static readonly StringAttribute Data = new(Namespace, nameof(Data));
    
    /// <summary>
    /// Encode the window data to a string
    /// </summary>
    public static string Encode(IDb db, WindowData windowData)
    {
        var options = db.Connection.ServiceProvider.GetRequiredService<JsonSerializerOptions>();
        return JsonSerializer.Serialize(windowData, options);
    }

    public partial struct ReadOnly
    {
        public WindowData WindowData
        {
            get
            {
                var options = Db.Connection.ServiceProvider.GetRequiredService<JsonSerializerOptions>();
                return JsonSerializer.Deserialize<WindowData>(Data, options)!;
            }
        }
    }
}
