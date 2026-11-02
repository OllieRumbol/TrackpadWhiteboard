using System.Runtime.InteropServices;
using TrackpadWhiteboard.App.Interop;

namespace TrackpadWhiteboard.App;

/// <summary>
/// Receives Raw Input from precision touchpads and follows one finger: the first one that touches.
/// </summary>
internal sealed class TouchpadReader
{
    private const ushort DigitizerPage = 0x0D;
    private const ushort TouchPadUsage = 0x05;

    private readonly Dictionary<IntPtr, TouchpadDevice?> _devices = [];
    private int _contactsLeftInFrame;
    private uint? _trackedContactId;

    public event Action<TouchpadPoint>? FingerDown;

    public event Action<TouchpadPoint>? FingerMoved;

    public event Action? FingerUp;

    public bool HasSeenTouchpad { get; private set; }

    /// <summary>
    /// Width divided by height of the touchpad that sent the latest input.
    /// </summary>
    public double AspectRatio { get; private set; } = 1.5;

    /// <summary>
    /// Asks Windows to send touchpad input to the window as WM_INPUT messages.
    /// </summary>
    public static bool Register(IntPtr windowHandle)
    {
        RawInputDevice[] devices =
        [
            new RawInputDevice { UsagePage = DigitizerPage, Usage = TouchPadUsage, Flags = 0, Target = windowHandle },
        ];

        return NativeMethods.RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RawInputDevice>());
    }

    /// <summary>
    /// Handles one WM_INPUT message. Its lParam is the raw input handle.
    /// </summary>
    public void HandleRawInput(IntPtr rawInputHandle)
    {
        var headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
        uint size = 0;
        NativeMethods.GetRawInputData(rawInputHandle, NativeMethods.RidInput, IntPtr.Zero, ref size, headerSize);
        if (size == 0)
        {
            return;
        }

        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (NativeMethods.GetRawInputData(rawInputHandle, NativeMethods.RidInput, buffer, ref size, headerSize) != size)
            {
                return;
            }

            ReadBuffer(buffer, (int)headerSize);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void ReadBuffer(IntPtr buffer, int headerSize)
    {
        var header = Marshal.PtrToStructure<RawInputHeader>(buffer);
        if (header.Type != NativeMethods.RimTypeHid)
        {
            return;
        }

        var device = GetDevice(header.Device);
        if (device is null)
        {
            return;
        }

        HasSeenTouchpad = true;
        AspectRatio = device.AspectRatio;

        // RAWHID: report size, report count, then the reports.
        var reportSize = Marshal.ReadInt32(buffer, headerSize);
        var reportCount = Marshal.ReadInt32(buffer, headerSize + 4);
        var firstReport = buffer + headerSize + 8;

        for (var i = 0; i < reportCount; i++)
        {
            var report = new byte[reportSize];
            Marshal.Copy(firstReport + (i * reportSize), report, 0, reportSize);
            ReadReport(device, report);
        }
    }

    private TouchpadDevice? GetDevice(IntPtr deviceHandle)
    {
        if (!_devices.TryGetValue(deviceHandle, out var device))
        {
            device = TouchpadDevice.TryCreate(deviceHandle);
            _devices[deviceHandle] = device;
        }

        return device;
    }

    private void ReadReport(TouchpadDevice device, byte[] report)
    {
        // A touchpad can split one frame of fingers over several reports.
        // The first report gives the finger count; the next reports give a count of zero.
        var contactCount = device.ReadContactCount(report);
        if (contactCount is not null)
        {
            _contactsLeftInFrame = contactCount.Value;
        }

        foreach (var collection in device.ContactCollections)
        {
            if (_contactsLeftInFrame == 0)
            {
                break;
            }

            _contactsLeftInFrame--;
            HandleContact(device.ReadContact(collection, report));
        }
    }

    private void HandleContact(TouchpadContact contact)
    {
        if (_trackedContactId is null)
        {
            if (contact.IsTouching)
            {
                _trackedContactId = contact.Id;
                FingerDown?.Invoke(contact.Position);
            }

            return;
        }

        if (contact.Id != _trackedContactId)
        {
            return;
        }

        if (contact.IsTouching)
        {
            FingerMoved?.Invoke(contact.Position);
        }
        else
        {
            _trackedContactId = null;
            FingerUp?.Invoke();
        }
    }
}
