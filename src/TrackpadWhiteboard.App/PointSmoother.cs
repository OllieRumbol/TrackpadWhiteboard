using System.Windows;

namespace TrackpadWhiteboard.App;

/// <summary>
/// Removes finger jitter from a stroke. Each new point moves only part of the way towards the raw point.
/// </summary>
internal sealed class PointSmoother
{
    private Point? _previous;

    public SmoothingLevel Level { get; set; } = SmoothingLevel.Medium;

    public void Reset() => _previous = null;

    public Point Smooth(Point raw)
    {
        if (_previous is null)
        {
            _previous = raw;
            return raw;
        }

        var follow = Level switch
        {
            SmoothingLevel.Off => 1.0,
            SmoothingLevel.Light => 0.6,
            SmoothingLevel.Medium => 0.35,
            SmoothingLevel.Strong => 0.2,
            _ => 1.0,
        };

        var previous = _previous.Value;
        var smoothed = new Point(
            previous.X + ((raw.X - previous.X) * follow),
            previous.Y + ((raw.Y - previous.Y) * follow));

        _previous = smoothed;
        return smoothed;
    }
}
