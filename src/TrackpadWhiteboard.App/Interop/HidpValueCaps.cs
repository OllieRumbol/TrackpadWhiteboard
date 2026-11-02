using System.Runtime.InteropServices;

namespace TrackpadWhiteboard.App.Interop;

/// <summary>
/// The fields of the HID HIDP_VALUE_CAPS structure that this app reads. The offsets follow hidpi.h.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 72)]
internal struct HidpValueCaps
{
    [FieldOffset(0)]
    public ushort UsagePage;

    [FieldOffset(6)]
    public ushort LinkCollection;

    [FieldOffset(40)]
    public int LogicalMin;

    [FieldOffset(44)]
    public int LogicalMax;

    [FieldOffset(48)]
    public int PhysicalMin;

    [FieldOffset(52)]
    public int PhysicalMax;

    // NotRange.Usage, or Range.UsageMin when the caps describe a usage range.
    [FieldOffset(56)]
    public ushort Usage;
}
