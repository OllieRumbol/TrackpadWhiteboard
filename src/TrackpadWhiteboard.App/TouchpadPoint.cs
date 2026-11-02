namespace TrackpadWhiteboard.App;

/// <summary>
/// A finger position on the touchpad. X and Y go from 0 (left, top) to 1 (right, bottom).
/// </summary>
internal readonly record struct TouchpadPoint(double X, double Y);
