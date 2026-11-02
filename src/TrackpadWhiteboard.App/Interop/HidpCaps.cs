using System.Runtime.InteropServices;

namespace TrackpadWhiteboard.App.Interop;

/// <summary>
/// The fields of the HID HIDP_CAPS structure that this app reads. The offsets follow hidpi.h.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 64)]
internal struct HidpCaps
{
    [FieldOffset(48)]
    public ushort NumberInputValueCaps;
}
