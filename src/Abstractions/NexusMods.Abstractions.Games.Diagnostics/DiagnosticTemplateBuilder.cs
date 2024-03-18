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
        IWithTitle WithId(DiagnosticId id);
    }

    /// <summary>
    /// Title step.
    /// </summary>
    public interface IWithTitle
    {
        /// <summary>
        /// Sets <see cref="Diagnostic.Title"/>.
        /// </summary>
        IWithSeverityStep WithTitle(string title);
    }

    /// <summary>
    /// Severity Step.
    /// </summary>
    public interface IWithSeverityStep
    {
        /// <summary>
        /// Sets the <see cref="DiagnosticSeverity"/>.
        /// </summary>
        IWithSummaryStep WithSeverity(DiagnosticSeverity severity);
    }

    /// <summary>
    /// Summary step.
    /// </summary>
    public interface IWithSummaryStep
    {
        /// <summary>
        /// Sets the message template for the <see cref="Diagnostic.Summary"/> property.
        /// </summary>
        IWithDetailsStep WithSummary(string message);
    }

    /// <summary>
    /// Details step.
    /// </summary>
    public interface IWithDetailsStep
    {
        /// <summary>
        /// Sets the message template for the <see cref="Diagnostic.Details"/> property.
        /// </summary>
        IWithMessageData WithDetails(string message);

        /// <summary>
        /// Sets nothing for the <see cref="Diagnostic.Details"/> property.
        /// </summary>
        IWithMessageData WithoutDetails();
    }

    /// <summary>
    /// Message data step.
    /// </summary>
    public interface IWithMessageData
    {
        /// <summary>
        /// Configures the message builder.
        /// </summary>
        IFinishStep WithMessageData(Func<IMessageBuilder, IMessageBuilder> messageBuilder);
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

        /// <summary>
        /// Adds a simple value to the message.
        /// </summary>
        IMessageBuilder AddValue<T>(string name) where T : notnull;
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
