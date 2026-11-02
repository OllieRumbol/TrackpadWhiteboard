using System.Runtime.InteropServices;
using TrackpadWhiteboard.App.Interop;

namespace TrackpadWhiteboard.App;

/// <summary>
/// Reads finger contacts from the HID reports of one precision touchpad.
/// </summary>
internal sealed class TouchpadDevice
{
    private const ushort GenericDesktopPage = 0x01;
    private const ushort XUsage = 0x30;
    private const ushort YUsage = 0x31;
    private const ushort DigitizerPage = 0x0D;
    private const ushort TipSwitchUsage = 0x42;
    private const ushort ContactIdUsage = 0x51;
    private const ushort ContactCountUsage = 0x54;
    private const ushort TopLevelCollection = 0;
    private const int MaxUsagesPerCollection = 32;

    private readonly IntPtr _preparsedData;

    private TouchpadDevice(IntPtr preparsedData, IReadOnlyList<ContactCollection> contactCollections, double aspectRatio)
    {
        _preparsedData = preparsedData;
        ContactCollections = contactCollections;
        AspectRatio = aspectRatio;
    }

    public IReadOnlyList<ContactCollection> ContactCollections { get; }

    /// <summary>
    /// Width divided by height of the touch surface.
    /// </summary>
    public double AspectRatio { get; }

    public static TouchpadDevice? TryCreate(IntPtr deviceHandle)
    {
        var preparsedData = ReadPreparsedData(deviceHandle);
        if (preparsedData == IntPtr.Zero)
        {
            return null;
        }

        var valueCaps = ReadInputValueCaps(preparsedData);
        var contactCollections = new List<ContactCollection>();
        HidpValueCaps? firstX = null;
        HidpValueCaps? firstY = null;

        foreach (var xCaps in valueCaps.Where(caps => caps.UsagePage == GenericDesktopPage && caps.Usage == XUsage))
        {
            var yCaps = valueCaps.FirstOrDefault(caps =>
                caps.UsagePage == GenericDesktopPage && caps.Usage == YUsage && caps.LinkCollection == xCaps.LinkCollection);

            contactCollections.Add(new ContactCollection(
                xCaps.LinkCollection, xCaps.LogicalMin, xCaps.LogicalMax, yCaps.LogicalMin, yCaps.LogicalMax));

            firstX ??= xCaps;
            firstY ??= yCaps;
        }

        if (firstX is null || firstY is null)
        {
            Marshal.FreeHGlobal(preparsedData);
            return null;
        }

        // The preparsed data stays allocated while the app runs, because every report needs it.
        return new TouchpadDevice(preparsedData, contactCollections, CalculateAspectRatio(firstX.Value, firstY.Value));
    }

    /// <summary>
    /// Returns the number of fingers that the report starts, or null when the report continues an earlier one.
    /// </summary>
    public int? ReadContactCount(byte[] report)
    {
        var count = ReadValue(DigitizerPage, TopLevelCollection, ContactCountUsage, report);
        return count is null or 0 ? null : (int)count.Value;
    }

    public TouchpadContact ReadContact(ContactCollection collection, byte[] report)
    {
        var id = ReadValue(DigitizerPage, collection.LinkCollection, ContactIdUsage, report) ?? collection.LinkCollection;
        var x = ReadValue(GenericDesktopPage, collection.LinkCollection, XUsage, report) ?? (uint)collection.XMin;
        var y = ReadValue(GenericDesktopPage, collection.LinkCollection, YUsage, report) ?? (uint)collection.YMin;
        var isTouching = IsUsageOn(DigitizerPage, collection.LinkCollection, TipSwitchUsage, report);

        var position = new TouchpadPoint(
            Normalize(x, collection.XMin, collection.XMax),
            Normalize(y, collection.YMin, collection.YMax));

        return new TouchpadContact(id, isTouching, position);
    }

    private static IntPtr ReadPreparsedData(IntPtr deviceHandle)
    {
        uint size = 0;
        NativeMethods.GetRawInputDeviceInfo(deviceHandle, NativeMethods.RidiPreparsedData, IntPtr.Zero, ref size);
        if (size == 0)
        {
            return IntPtr.Zero;
        }

        var preparsedData = Marshal.AllocHGlobal((int)size);
        var result = NativeMethods.GetRawInputDeviceInfo(deviceHandle, NativeMethods.RidiPreparsedData, preparsedData, ref size);
        if (result == uint.MaxValue)
        {
            Marshal.FreeHGlobal(preparsedData);
            return IntPtr.Zero;
        }

        return preparsedData;
    }

    private static HidpValueCaps[] ReadInputValueCaps(IntPtr preparsedData)
    {
        if (NativeMethods.HidPGetCaps(preparsedData, out var caps) != NativeMethods.HidpStatusSuccess)
        {
            return [];
        }

        var valueCaps = new HidpValueCaps[caps.NumberInputValueCaps];
        var length = caps.NumberInputValueCaps;
        if (NativeMethods.HidPGetValueCaps(NativeMethods.HidpInputReport, valueCaps, ref length, preparsedData) != NativeMethods.HidpStatusSuccess)
        {
            return [];
        }

        return valueCaps[..length];
    }

    private static double CalculateAspectRatio(HidpValueCaps x, HidpValueCaps y)
    {
        // Physical ranges share one unit, so they give the true shape of the pad.
        // Logical ranges are a fallback, because X and Y can have different resolutions.
        double physicalWidth = x.PhysicalMax - x.PhysicalMin;
        double physicalHeight = y.PhysicalMax - y.PhysicalMin;
        if (physicalWidth > 0 && physicalHeight > 0)
        {
            return physicalWidth / physicalHeight;
        }

        double logicalWidth = x.LogicalMax - x.LogicalMin;
        double logicalHeight = y.LogicalMax - y.LogicalMin;
        return logicalHeight > 0 ? logicalWidth / logicalHeight : 1.5;
    }

    private uint? ReadValue(ushort usagePage, ushort linkCollection, ushort usage, byte[] report)
    {
        var status = NativeMethods.HidPGetUsageValue(
            NativeMethods.HidpInputReport, usagePage, linkCollection, usage, out var value, _preparsedData, report, (uint)report.Length);

        return status == NativeMethods.HidpStatusSuccess ? value : null;
    }

    private bool IsUsageOn(ushort usagePage, ushort linkCollection, ushort usage, byte[] report)
    {
        var usages = new ushort[MaxUsagesPerCollection];
        var length = (uint)usages.Length;
        var status = NativeMethods.HidPGetUsages(
            NativeMethods.HidpInputReport, usagePage, linkCollection, usages, ref length, _preparsedData, report, (uint)report.Length);

        return status == NativeMethods.HidpStatusSuccess && usages.Take((int)length).Contains(usage);
    }

    private static double Normalize(uint value, int min, int max)
    {
        if (max <= min)
        {
            return 0;
        }

        return Math.Clamp(((double)value - min) / (max - min), 0, 1);
    }
}
