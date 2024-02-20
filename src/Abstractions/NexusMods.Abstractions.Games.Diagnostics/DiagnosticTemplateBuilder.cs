#define DiagnosticTemplateBuilderExperimentTestOutput

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics.References;

namespace NexusMods.Abstractions.Diagnostics;

/// <summary>
/// A builder used at compile time by a source generator to create a diagnostic template.
/// </summary>
/// <remarks>
/// This should never be used at runtime!
/// </remarks>
/// <seealso cref="IDiagnosticTemplate"/>
[PublicAPI]
public static class DiagnosticTemplateBuilder
{
    /// <summary>
    /// Start of the builder chain.
    /// </summary>
    public static IWithIdStep Start() => ThrowException();

    [DoesNotReturn]
    [ContractAnnotation("=> halt")]
    private static IWithIdStep ThrowException()
    {
        throw new UnreachableException($"The {nameof(DiagnosticTemplateBuilder)} is not supposed to be used at runtime!");
    }

    /// <summary>
    /// ID step.
    /// </summary>
    public interface IWithIdStep
    {
        /// <summary>
        /// Sets the <see cref="DiagnosticId"/>.
        /// </summary>
        IWithSeverityStep WithId(DiagnosticId id);
    }

    /// <summary>
    /// Severity Step.
    /// </summary>
    public interface IWithSeverityStep
    {
        /// <summary>
        /// Sets the <see cref="DiagnosticSeverity"/>.
        /// </summary>
        IWithMessageStep WithSeverity(DiagnosticSeverity severity);
    }

    /// <summary>
    /// Message Step.
    /// </summary>
    public interface IWithMessageStep
    {
        /// <summary>
        /// Sets the message template.
        /// </summary>
        IFinishStep WithMessage(string message, Func<IMessageBuilder, IMessageBuilder> messageBuilder);
    }

    /// <summary>
    /// Message Builder.
    /// </summary>
    public interface IMessageBuilder
    {
        /// <summary>
        /// Adds data references to the message.
        /// </summary>
        IMessageBuilder AddDataReference<T>(string name) where T : IDataReference;
    }

    /// <summary>
    /// Finish step.
    /// </summary>
    public interface IFinishStep
    {
        /// <summary>
        /// Ends the builder.
        /// </summary>
        IDiagnosticTemplate Finish();
    }
}

/// <summary>
/// Output of <see cref="DiagnosticTemplateBuilder"/>.
/// </summary>
/// <seealso cref="DiagnosticTemplateBuilder"/>
public interface IDiagnosticTemplate;

#if DiagnosticTemplateBuilderExperimentTestOutput
#if RELEASE
#else

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
internal partial class Test
{
    private static readonly IDiagnosticTemplate Diagnostic1Template = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(source: "MyCoolSource", number: 13))
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithMessage("Mod '{Mod}' is not working!", messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
        )
        .Finish();
}

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
internal partial class Test
{
    public static Diagnostic<Diagnostic1MessageData> CreateDiagnostic1(ModReference mod)
    {
        var messageData = new Diagnostic1MessageData(mod);

        return new Diagnostic<Diagnostic1MessageData>
        {
            Id = new DiagnosticId(source: "MyCoolSource", number: 13),
            Severity = DiagnosticSeverity.Warning,
            Message = DiagnosticMessage.From("Mod '{Mod}' is not working!"),
            MessageData = messageData,
            DataReferences = new Dictionary<DataReferenceDescription, IDataReference>
            {
                { DataReferenceDescription.From("Mod"), messageData.Mod },
            },
        };
    }

    public readonly struct Diagnostic1MessageData
    {
        public readonly ModReference Mod;

        public Diagnostic1MessageData(ModReference mod)
        {
            Mod = mod;
        }
    }
}
#endif
#endif
