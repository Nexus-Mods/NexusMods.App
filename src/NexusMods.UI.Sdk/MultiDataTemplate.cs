using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace NexusMods.UI.Sdk;

public class MultiDataTemplate : IDataTemplate
{
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global", Justification = "Updated in XAML")]
    public List<ITypedDataTemplate> AvailableTemplates { get; } = [];

    public Control? Fallback { get; set; }
    
    public Control? Build(object? param)
    {
        foreach (var template in AvailableTemplates)
        {
            if (template.Match(param)) return template.Build(param);
        }

        return Fallback;
    }

    public bool Match(object? data) => true;
}
