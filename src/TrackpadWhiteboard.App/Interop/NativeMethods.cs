using System.Runtime.InteropServices;

namespace TrackpadWhiteboard.App.Interop;

/// <summary>
/// Win32 calls for Raw Input, the HID parser and the mouse cursor.
/// </summary>
internal static partial class NativeMethods
{
    public const int WmInput = 0x00FF;
    public const uint RidInput = 0x10000003;
    public const uint RidiPreparsedData = 0x20000005;
    public const uint RimTypeHid = 2;
    public const int HidpStatusSuccess = 0x00110000;
    public const int HidpInputReport = 0;

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterRawInputDevices(RawInputDevice[] devices, uint deviceCount, uint deviceSize);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint GetRawInputData(IntPtr rawInput, uint command, IntPtr data, ref uint size, uint headerSize);

    [LibraryImport("user32.dll", EntryPoint = "GetRawInputDeviceInfoW", SetLastError = true)]
    public static partial uint GetRawInputDeviceInfo(IntPtr device, uint command, IntPtr data, ref uint size);

    [LibraryImport("hid.dll", EntryPoint = "HidP_GetCaps")]
    public static partial int HidPGetCaps(IntPtr preparsedData, out HidpCaps capabilities);

    [LibraryImport("hid.dll", EntryPoint = "HidP_GetValueCaps")]
    public static partial int HidPGetValueCaps(int reportType, [Out] HidpValueCaps[] valueCaps, ref ushort valueCapsLength, IntPtr preparsedData);

    [LibraryImport("hid.dll", EntryPoint = "HidP_GetUsageValue")]
    public static partial int HidPGetUsageValue(int reportType, ushort usagePage, ushort linkCollection, ushort usage, out uint usageValue, IntPtr preparsedData, byte[] report, uint reportLength);

    [LibraryImport("hid.dll", EntryPoint = "HidP_GetUsages")]
    public static partial int HidPGetUsages(int reportType, ushort usagePage, ushort linkCollection, [Out] ushort[] usageList, ref uint usageLength, IntPtr preparsedData, byte[] report, uint reportLength);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ClipCursor(ref NativeRect rect);

    // ClipCursor with a null rectangle lets the cursor move freely again.
    [LibraryImport("user32.dll", EntryPoint = "ClipCursor", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReleaseCursorClip(IntPtr rect);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out NativePoint point);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetCursorPos(int x, int y);
}
