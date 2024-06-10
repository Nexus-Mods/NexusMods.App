using System.Diagnostics.CodeAnalysis;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

[Include<File>]
public partial class GeneratedFile : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Loadouts.Files.GeneratedFile";
    
    /// <summary>
    /// The generator unique ID for the file, this specifies the class that is used to generate
    /// the contents of this file. 
    /// </summary>
    public static readonly ClassAttribute<IFileGenerator> Generator = new(Namespace, nameof(Generator)) { IsIndexed = true };

    public partial struct ReadOnly
    {
        /// <summary>
        /// The set instance of the generator that is used to generate the contents of this file.
        /// </summary>
        public IFileGenerator GeneratorInstance => GeneratedFile.Generator.GetInstance(this);
    }
}
