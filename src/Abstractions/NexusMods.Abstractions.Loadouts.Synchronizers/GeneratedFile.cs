using System.Diagnostics.CodeAnalysis;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

public static class GeneratedFile
{
    private const string Namespace = "NexusMods.Abstractions.Loadouts.Files.GeneratedFile";
    
    /// <summary>
    /// The generator unique ID for the file, this specifies the class that is used to generate
    /// the contents of this file. 
    /// </summary>
    public static readonly ClassAttribute<IFileGenerator> Generator = new(Namespace, nameof(Generator)) { IsIndexed = true };

    public class Model(ITransaction tx) : File.Model(tx)
    {
        /// <summary>
        /// Get an instance of the generator for this file.
        /// </summary>
        public IFileGenerator Generator
        {
            get => GeneratedFile.Generator.GetInstance(this);
        }
        
        /// <summary>
        /// Set the generator for this file.
        /// </summary>
        public void SetGenerator<TType>()
        where TType : IFileGenerator
        {
            GeneratedFile.Generator.Add(this, TType.Guid);
        }
    }
    
    #region Remappers
    
    /// <summary>
    /// If this file is a generated file, this will return true and cast the generated file to the out parameter.
    /// </summary>
    public static bool TryGetAsGeneratedFile(this File.Model model, [NotNullWhen(true)] out GeneratedFile.Model? generatedFile)
    {
        generatedFile = null;
        if (!model.Contains(GeneratedFile.Generator))
            return false;

        generatedFile = model.Remap<GeneratedFile.Model>();
        return true;
    }

    /// <summary>
    /// Returns true if the file is a generated file of the specified type.
    /// </summary>
    public static bool IsGeneratedFile<TType>(this File.Model model)
        where TType : IFileGenerator =>
        model.TryGet(GeneratedFile.Generator, out var val) && val == TType.Guid;
    
    /// <summary>
    /// Returns true if the file is a generated file.
    /// </summary>
    public static bool IsGeneratedFile(this File.Model model) =>
        model.Contains(GeneratedFile.Generator);
    
    #endregion
}
