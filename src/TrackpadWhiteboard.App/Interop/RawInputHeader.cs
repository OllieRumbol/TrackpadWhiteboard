using System.Runtime.InteropServices;

namespace TrackpadWhiteboard.App.Interop;

/// <summary>
/// The Win32 RAWINPUTHEADER structure. The RAWHID data follows it in the same buffer.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct RawInputHeader
{
    public uint Type;
    public uint Size;
    public IntPtr Device;
    public IntPtr WParam;
}
