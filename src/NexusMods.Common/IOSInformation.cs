using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using static NexusMods.Common.Delegates;

namespace NexusMods.Common;

/// <summary>
/// Provides information about the current operating system.
/// </summary>
[PublicAPI]
public interface IOSInformation
{
    /// <summary>
    /// The current <see cref="OSPlatform"/>.
    /// </summary>
    OSPlatform Platform { get; }

    /// <summary>
    /// Whether the current <see cref="Platform"/> is <see cref="OSPlatform.Windows"/>.
    /// </summary>
    bool IsWindows => Platform == OSPlatform.Windows;

    /// <summary>
    /// Whether the current <see cref="Platform"/> is <see cref="OSPlatform.Linux"/>.
    /// </summary>
    bool IsLinux => Platform == OSPlatform.Linux;

    /// <summary>
    /// Whether the current <see cref="Platform"/> is <see cref="OSPlatform.OSX"/>.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = $"It's also named {nameof(OSPlatform.OSX)} in {nameof(OSPlatform)}")]
    bool IsOSX => Platform == OSPlatform.OSX;

    /// <summary>
    /// Matches and returns a value based on the current platform.
    /// </summary>
    /// <param name="onWindows"></param>
    /// <param name="onLinux"></param>
    /// <param name="onOSX"></param>
    /// <typeparam name="TOut"></typeparam>
    /// <returns></returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the current platform is not supported.</exception>
    /// <seealso cref="MatchPlatform{TOut}"/>
    /// <seealso cref="SwitchPlatform"/>
    /// <seealso cref="SwitchPlatform{TState}"/>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = $"It's also named {nameof(OSPlatform.OSX)} in {nameof(OSPlatform)}")]
    TOut MatchPlatform<TOut>(Func<TOut> onWindows, Func<TOut> onLinux, Func<TOut> onOSX)
    {
        if (IsWindows) return onWindows();
        if (IsLinux) return onLinux();
        if (IsOSX) return onOSX();
        throw CreatePlatformNotSupportedException();
    }

    /// <summary>
    /// Matches and returns a value based on the current platform and allows
    /// <paramref name="state"/> to be passed to the each handler, preventing lambda allocations.
    /// </summary>
    /// <param name="onWindows"></param>
    /// <param name="onLinux"></param>
    /// <param name="onOSX"></param>
    /// <param name="state"></param>
    /// <typeparam name="TState"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    /// <returns></returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the current platform is not supported.</exception>
    /// <seealso cref="MatchPlatform{TOut}"/>
    /// <seealso cref="SwitchPlatform"/>
    /// <seealso cref="SwitchPlatform{TState}"/>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = $"It's also named {nameof(OSPlatform.OSX)} in {nameof(OSPlatform)}")]
    TOut MatchPlatform<TState, TOut>(
        FuncRef<TState, TOut> onWindows,
        FuncRef<TState, TOut> onLinux,
        FuncRef<TState, TOut> onOSX,
        ref TState state)
    {
        if (IsWindows) return onWindows(ref state);
        if (IsLinux) return onLinux(ref state);
        if (IsOSX) return onOSX(ref state);
        throw CreatePlatformNotSupportedException();
    }

    /// <summary>
    /// Switches on the current platform.
    /// </summary>
    /// <param name="onWindows"></param>
    /// <param name="onLinux"></param>
    /// <param name="onOSX"></param>
    /// <exception cref="PlatformNotSupportedException">Thrown when the current platform is not supported.</exception>
    /// <seealso cref="SwitchPlatform{TState}"/>
    /// <seealso cref="MatchPlatform{TOut}"/>
    /// <seealso cref="MatchPlatform{TState, TOut}"/>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = $"It's also named {nameof(OSPlatform.OSX)} in {nameof(OSPlatform)}")]
    void SwitchPlatform(Action onWindows, Action onLinux, Action onOSX)
    {
        if (IsWindows)
        {
            onWindows();
            return;
        }

        if (IsLinux)
        {
            onLinux();
            return;
        }

        if (IsOSX)
        {
            onOSX();
            return;
        }

        throw CreatePlatformNotSupportedException();
    }

    /// <summary>
    /// Switches on the current platform and allows <paramref name="state"/> to be
    /// passed to the each handler, preventing lambda allocations.
    /// </summary>
    /// <param name="onWindows"></param>
    /// <param name="onLinux"></param>
    /// <param name="onOSX"></param>
    /// <param name="state"></param>
    /// <typeparam name="TState"></typeparam>
    /// <exception cref="PlatformNotSupportedException">Thrown when the current platform is not supported.</exception>
    /// <seealso cref="SwitchPlatform"/>
    /// <seealso cref="MatchPlatform{TOut}"/>
    /// <seealso cref="MatchPlatform{TState, TOut}"/>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = $"It's also named {nameof(OSPlatform.OSX)} in {nameof(OSPlatform)}")]
    void SwitchPlatform<TState>(
        ActionRef<TState> onWindows,
        ActionRef<TState> onLinux,
        ActionRef<TState> onOSX,
        ref TState state)
    {
        if (IsWindows)
        {
            onWindows(ref state);
            return;
        }

        if (IsLinux)
        {
            onLinux(ref state);
            return;
        }

        if (IsOSX)
        {
            onOSX(ref state);
            return;
        }

        throw CreatePlatformNotSupportedException();
    }

    /// <summary>
    /// Returns <c>true</c> if the current platform <see cref="Platform"/> is supported.
    /// </summary>
    /// <returns></returns>
    bool IsPlatformSupported()
    {
        return IsWindows || IsLinux || IsOSX;
    }

    /// <summary>
    /// Guard statement for platform support.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Thrown when the current platform is not supported.</exception>
    void PlatformSupportedGuard()
    {
        if (!IsPlatformSupported())
            throw CreatePlatformNotSupportedException();
    }

    /// <summary>
    /// Creates a new <see cref="PlatformNotSupportedException"/>.
    /// </summary>
    /// <returns></returns>
    PlatformNotSupportedException CreatePlatformNotSupportedException()
    {
        return new PlatformNotSupportedException($"The current platform is not supported: {Platform}");
    }
}
