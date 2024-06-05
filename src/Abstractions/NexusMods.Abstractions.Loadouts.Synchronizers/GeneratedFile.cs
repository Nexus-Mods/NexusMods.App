using System.Diagnostics.CodeAnalysis;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

[Include<File>]
public partial class GeneratedFile
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
        /// Get an instance of the generator for this file.
        /// </summary>
        public IFileGenerator GeneratorInstance
        {
            get => Generator.GetInstance(this);
        }
        
        /// <summary>
        /// Set the generator for this file.
        /// </summary>
        public void SetGenerator<TType>()
        where TType : IFileGenerator
        {
            Generator.Add(Id, TType.Guid);
        }
    }
    
    #region Remappers
    
    /// <summary>
    /// If this file is a generated file, this will return true and cast the generated file to the out parameter.
    /// </summary>
    public static bool TryGetAsGeneratedFile(this File.ReadOnly model, [NotNullWhen(true)] out GeneratedFile.ReadOnly? generatedFile)
    {
        generatedFile = null;
        if (!model.Contains(GeneratedFile.Generator))
            return false;

        generatedFile = model.Remap<GeneratedFile.ReadOnly>();
        return true;
    }

    /// <summary>
    /// Returns true if the file is a generated file of the specified type.
    /// </summary>
    public static bool IsGeneratedFile<TType>(this File.ReadOnly model)
        where TType : IFileGenerator =>
        model.TryGet(GeneratedFile.Generator, out var val) && val == TType.Guid;
    
    /// <summary>
    /// Returns true if the file is a generated file.
    /// </summary>
    public static bool IsGeneratedFile(this File.ReadOnly model) =>
        model.Contains(GeneratedFile.Generator);
    
    #endregion
}
