using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

public static class GeneratedFile
{
    private const string Namespace = "NexusMods.Abstractions.Loadouts.Files.GeneratedFile";
    
    public static readonly ClassAttribute<IFileGenerator> Generator = new("NexusMods.Abstractions.Loadouts.Files.GeneratedFile", nameof(Generator));

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
        public void SetGenerator<TType>(TType generator)
        where TType : IFileGenerator
        {
            GeneratedFile.Generator.Add(this, generator);
        }
    }
}

/// <summary>
/// Extensions for the <see cref="File.Model"/> class.
/// </summary>
public static class FileModelExtensions
{
    /// <summary>
    /// If this file is a generated file, this will return true and cast the generated file to the out parameter.
    /// </summary>
    public static bool IsGeneratedFile(this File.Model model, out GeneratedFile.Model generatedFile)
    {
        generatedFile = model.Remap<GeneratedFile.Model>();
        return model.Contains(GeneratedFile.Generator);
    }
}
