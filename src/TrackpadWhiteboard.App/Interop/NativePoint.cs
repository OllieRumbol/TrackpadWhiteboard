using System.Runtime.InteropServices;

namespace TrackpadWhiteboard.App.Interop;

/// <summary>
/// The Win32 POINT structure, in screen pixels.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal record struct NativePoint(int X, int Y);
