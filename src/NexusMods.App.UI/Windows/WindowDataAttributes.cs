
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.App.UI.Windows;

public class WindowDataAttributes
{
    private const string Namespace = "NexusMods.App.UI.Windows.WindowData";
    
    /// <summary>
    /// The state of the window, stored as json
    /// </summary>
    public static readonly StringAttribute Data = new(Namespace, "Data");

    public class Model(ITransaction tx) : Entity(tx)
    {
        /// <summary>
        /// The data of the window
        /// </summary>
        public WindowData Data
        {
            get
            {
                var options = Db.Connection.ServiceProvider.GetRequiredService<JsonSerializerOptions>();
                var jsonString = WindowDataAttributes.Data.Get(this);
                return JsonSerializer.Deserialize<WindowData>(jsonString, options)!;
            }
            set
            {
                var options = Db.Connection.ServiceProvider.GetRequiredService<JsonSerializerOptions>();
                var jsonString = JsonSerializer.Serialize(value, options);
                WindowDataAttributes.Data.Add(this, jsonString);
            }
        }
    }
}
