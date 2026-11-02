using System.Runtime.InteropServices;

namespace TrackpadWhiteboard.App.Interop;

/// <summary>
/// The Win32 RAWINPUTDEVICE structure.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct RawInputDevice
{
    public ushort UsagePage;
    public ushort Usage;
    public uint Flags;
    public IntPtr Target;
}
