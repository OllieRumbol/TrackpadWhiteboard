using System.Windows;

namespace TrackpadWhiteboard.App;

/// <summary>
/// Places touchpad strokes along a line of writing on the board.
/// The touchpad maps to a "writing window" on the line. When the writer moves along, the window moves to the right of the ink.
/// </summary>
internal sealed class LineWriter
{
    private const double SpaceWidthRatio = 0.6;
    private const double LineSpacingRatio = 1.3;

    // In Edge mode, a jump to the left of more than this part of the pad width moves the window along.
    private const double EdgeJumpThreshold = 0.4;

    private double _lineLeft;
    private double _windowLeft;
    private double _strokeCaretX;
    private double _inkRight;
    private bool _gapBeforeNextStroke;
    private bool _startAfterInk = true;
    private double _strokeRightOnPad;
    private double _previousStrokeRightOnPad;
    private TimeSpan? _lastFingerUp;

    public double LineHeight { get; set; } = 90;

    /// <summary>
    /// The gap between letters when the writer moves along the line, as a part of the line height.
    /// </summary>
    public double LetterGapRatio { get; set; } = 0.3;

    public double PadAspectRatio { get; set; } = 1.5;

    public AdvanceMode AdvanceMode { get; set; } = AdvanceMode.EdgeOrPause;

    /// <summary>
    /// In Pause mode, how long the finger must stay off the pad before the next stroke moves along.
    /// </summary>
    public TimeSpan PauseBeforeMoveAlong { get; set; } = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// How long the finger was off the pad before the latest stroke. Null before the first stroke.
    /// </summary>
    public TimeSpan? LastOffPadTime { get; private set; }

    public bool LastStrokeMovedAlong { get; private set; }

    public double LineTop { get; private set; }

    /// <summary>
    /// The area of the board that the whole touchpad maps to.
    /// </summary>
    public Rect WritingWindow => new(_windowLeft, LineTop, WindowWidth, LineHeight);

    /// <summary>
    /// Where the next stroke starts when the writer moves along the line.
    /// </summary>
    public double CaretX => _gapBeforeNextStroke ? _inkRight + LetterGap : _inkRight;

    private double WindowWidth => LineHeight * PadAspectRatio;

    private double LetterGap => LineHeight * LetterGapRatio;

    /// <summary>
    /// Starts a new line. The point is the left end of the line, at its vertical middle.
    /// </summary>
    public void StartLine(Point start)
    {
        _lineLeft = start.X;
        LineTop = start.Y - (LineHeight / 2);
        ClearLine();
    }

    public void NewLine()
    {
        LineTop += LineHeight * LineSpacingRatio;
        ClearLine();
    }

    public void AddSpace()
    {
        _inkRight = CaretX + (LineHeight * SpaceWidthRatio);
        _gapBeforeNextStroke = false;
        _startAfterInk = true;
    }

    /// <summary>
    /// Sets the right edge of the ink on this line after strokes were removed. Null means the line has no ink.
    /// </summary>
    public void SetInkRight(double? inkRight)
    {
        _inkRight = inkRight ?? _lineLeft;
        _gapBeforeNextStroke = inkRight is not null;
        _startAfterInk = true;
    }

    public Point BeginStroke(TouchpadPoint finger, TimeSpan now)
    {
        LastOffPadTime = now - _lastFingerUp;
        LastStrokeMovedAlong = ShouldMoveAlong(finger, now);

        if (LastStrokeMovedAlong)
        {
            // Place the window so that this first point lands at the caret.
            _strokeCaretX = CaretX;
            _windowLeft = _strokeCaretX - (finger.X * WindowWidth);
        }

        _startAfterInk = false;
        _strokeRightOnPad = finger.X;
        return ToBoard(finger);
    }

    public Point ContinueStroke(TouchpadPoint finger)
    {
        _strokeRightOnPad = Math.Max(_strokeRightOnPad, finger.X);
        return ToBoard(finger);
    }

    /// <summary>
    /// Ends the stroke and returns how far to move it to the right.
    /// A letter like "O" starts at the top and curves left, past the caret and into the gap.
    /// So a stroke that moved along is shifted until its left edge, not its first point, is at the caret.
    /// </summary>
    public double EndStroke(double strokeLeft, double strokeRight, TimeSpan now)
    {
        var shift = 0.0;
        if (LastStrokeMovedAlong && strokeLeft < _strokeCaretX)
        {
            shift = _strokeCaretX - strokeLeft;

            // Move the window too, so that the next strokes of this letter line up with the shifted stroke.
            _windowLeft += shift;
        }

        _inkRight = Math.Max(_inkRight, strokeRight + shift);
        _gapBeforeNextStroke = true;
        _previousStrokeRightOnPad = _strokeRightOnPad;
        _lastFingerUp = now;

        return shift;
    }

    private bool ShouldMoveAlong(TouchpadPoint finger, TimeSpan now)
    {
        if (_startAfterInk)
        {
            return true;
        }

        var jumpedFarLeft = _previousStrokeRightOnPad - finger.X > EdgeJumpThreshold;

        // A stroke that starts to the right of the ink cannot overlap it, so slow writing to the right stays in place.
        var startsLeftOfInk = ToBoard(finger).X < _inkRight;
        var pausedLongEnough = _lastFingerUp is null || now - _lastFingerUp > PauseBeforeMoveAlong;
        var pausedBeforeOverlap = pausedLongEnough && startsLeftOfInk;

        return AdvanceMode switch
        {
            AdvanceMode.Edge => jumpedFarLeft,
            AdvanceMode.Pause => pausedBeforeOverlap,
            AdvanceMode.EdgeOrPause => jumpedFarLeft || pausedBeforeOverlap,
            _ => false,
        };
    }

    private void ClearLine()
    {
        _inkRight = _lineLeft;
        _windowLeft = _lineLeft;
        _gapBeforeNextStroke = false;
        _startAfterInk = true;
    }

    private Point ToBoard(TouchpadPoint finger) =>
        new(_windowLeft + (finger.X * WindowWidth), LineTop + (finger.Y * LineHeight));
}
