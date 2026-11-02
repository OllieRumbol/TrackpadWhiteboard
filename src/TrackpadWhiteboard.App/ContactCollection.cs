namespace TrackpadWhiteboard.App;

/// <summary>
/// The HID link collection that holds one finger's data, with the range of its X and Y values.
/// </summary>
internal sealed record ContactCollection(ushort LinkCollection, int XMin, int XMax, int YMin, int YMax);
