using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using static NexusMods.Paths.Delegates;

namespace NexusMods.Paths;

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
    TOut MatchPlatform<TOut>(
        Func<TOut>? onWindows = null,
        Func<TOut>? onLinux = null,
        Func<TOut>? onOSX = null)
    {
        if (IsWindows) return onWindows is null ? throw CreatePlatformNotSupportedException() : onWindows();
        if (IsLinux) return onLinux is null ? throw CreatePlatformNotSupportedException() : onLinux();
        if (IsOSX) return onOSX is null ? throw CreatePlatformNotSupportedException() : onOSX();
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
        ref TState state,
        FuncRef<TState, TOut>? onWindows = null,
        FuncRef<TState, TOut>? onLinux = null,
        FuncRef<TState, TOut>? onOSX = null)
    {
        if (IsWindows) return onWindows is null ? throw CreatePlatformNotSupportedException() : onWindows(ref state);
        if (IsLinux) return onLinux is null ? throw CreatePlatformNotSupportedException() : onLinux(ref state);
        if (IsOSX) return onOSX is null ? throw CreatePlatformNotSupportedException() : onOSX(ref state);
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
    void SwitchPlatform(
        Action? onWindows = null,
        Action? onLinux = null,
        Action? onOSX = null)
    {
        if (IsWindows)
        {
            if (onWindows is null)
                throw CreatePlatformNotSupportedException();

            onWindows();
            return;
        }

        if (IsLinux)
        {
            if (onLinux is null)
                throw CreatePlatformNotSupportedException();

            onLinux();
            return;
        }

        if (IsOSX)
        {
            if (onOSX is null)
                throw CreatePlatformNotSupportedException();

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
        ref TState state,
        ActionRef<TState>? onWindows = null,
        ActionRef<TState>? onLinux = null,
        ActionRef<TState>? onOSX = null)
    {
        if (IsWindows)
        {
            if (onWindows is null)
                throw CreatePlatformNotSupportedException();

            onWindows(ref state);
            return;
        }

        if (IsLinux)
        {
            if (onLinux is null)
                throw CreatePlatformNotSupportedException();

            onLinux(ref state);
            return;
        }

        if (IsOSX)
        {
            if (onOSX is null)
                throw CreatePlatformNotSupportedException();

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
    /// Returns <c>true</c> if the current platform is Unix-based.
    /// </summary>
    /// <returns></returns>
    bool IsUnix() => IsLinux || IsOSX;

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
