namespace TrackpadWhiteboard.App;

/// <summary>
/// One finger in one touchpad report.
/// </summary>
internal readonly record struct TouchpadContact(uint Id, bool IsTouching, TouchpadPoint Position);
