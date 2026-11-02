namespace TrackpadWhiteboard.App;

/// <summary>
/// How the writer decides that a new stroke continues the line after the ink, instead of joining the letters already there.
/// </summary>
internal enum AdvanceMode
{
    /// <summary>The finger jumps far to the left on the pad, for example from the right edge back to the left edge.</summary>
    Edge,

    /// <summary>The finger stays off the pad for a short pause.</summary>
    Pause,

    /// <summary>Either of the above: a big jump moves along at once, and a pause handles short words.</summary>
    EdgeOrPause,
}
