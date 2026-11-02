using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using TrackpadWhiteboard.App.Interop;

namespace TrackpadWhiteboard.App;

/// <summary>
/// The whiteboard. Caps Lock on: write with one finger on the touchpad. Caps Lock off: normal mouse.
/// </summary>
public partial class MainWindow : Window
{
    private const double MinPointSpacing = 0.5;
    private const double MinPenThickness = 1;
    private const double MaxPenThickness = 30;
    private const double MinLineHeight = 40;
    private const double MaxLineHeight = 300;
    private const double LetterGapStep = 0.05;
    private const double MaxLetterGapRatio = 1;

    private static readonly TimeSpan PauseStep = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MinPause = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MaxPause = TimeSpan.FromSeconds(2);

    private readonly TouchpadReader _touchpad = new();
    private readonly LineWriter _writer = new();
    private readonly PointSmoother _smoother = new();
    private readonly List<Stroke> _currentLineStrokes = [];

    private Stroke? _activeStroke;
    private bool _isWriting;
    private NativePoint? _resumeCursor;
    private double _penThickness = 6;

    public MainWindow()
    {
        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
        Activated += (_, _) => SyncWritingModeWithCapsLock();
        Deactivated += (_, _) => StopWriting(moveCursorToCaret: false);
        PreviewKeyDown += OnPreviewKeyDown;
        PreviewKeyUp += OnPreviewKeyUp;

        _touchpad.FingerDown += OnFingerDown;
        _touchpad.FingerMoved += OnFingerMoved;
        _touchpad.FingerUp += FinishActiveStroke;

        UpdateStatus();
    }

    private static TimeSpan Now => TimeSpan.FromMilliseconds(Environment.TickCount64);

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(handle).AddHook(WindowProc);
        TouchpadReader.Register(handle);
    }

    private IntPtr WindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == NativeMethods.WmInput)
        {
            _touchpad.HandleRawInput(lParam);
        }

        return IntPtr.Zero;
    }

    private void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.CapsLock)
        {
            SyncWritingModeWithCapsLock();
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space when _isWriting:
                _writer.AddSpace();
                break;
            case Key.Enter when _isWriting:
                _writer.NewLine();
                _currentLineStrokes.Clear();
                break;
            case Key.Back:
                UndoLastStroke();
                break;
            case Key.Delete:
                ClearBoard();
                break;
            case Key.OemOpenBrackets:
                _penThickness = Math.Max(MinPenThickness, _penThickness - 1);
                break;
            case Key.OemCloseBrackets:
                _penThickness = Math.Min(MaxPenThickness, _penThickness + 1);
                break;
            case Key.OemMinus:
                _writer.LineHeight = Math.Max(MinLineHeight, _writer.LineHeight - 10);
                break;
            case Key.OemPlus:
                _writer.LineHeight = Math.Min(MaxLineHeight, _writer.LineHeight + 10);
                break;
            case Key.OemComma:
                _writer.LetterGapRatio = Math.Max(0, _writer.LetterGapRatio - LetterGapStep);
                break;
            case Key.OemPeriod:
                _writer.LetterGapRatio = Math.Min(MaxLetterGapRatio, _writer.LetterGapRatio + LetterGapStep);
                break;
            case Key.S:
                _smoother.Level = _smoother.Level == SmoothingLevel.Strong ? SmoothingLevel.Off : _smoother.Level + 1;
                break;
            case Key.A:
                _writer.AdvanceMode = _writer.AdvanceMode == AdvanceMode.EdgeOrPause ? AdvanceMode.Edge : _writer.AdvanceMode + 1;
                break;
            case Key.OemSemicolon:
                _writer.PauseBeforeMoveAlong = Max(MinPause, _writer.PauseBeforeMoveAlong - PauseStep);
                break;
            case Key.OemQuotes:
                _writer.PauseBeforeMoveAlong = Min(MaxPause, _writer.PauseBeforeMoveAlong + PauseStep);
                break;
            default:
                return;
        }

        e.Handled = true;
        UpdateOverlay();
        UpdateStatus();
    }

    private void SyncWritingModeWithCapsLock()
    {
        var capsLockOn = Keyboard.IsKeyToggled(Key.CapsLock);
        if (capsLockOn && !_isWriting)
        {
            StartWriting();
        }
        else if (!capsLockOn && _isWriting)
        {
            StopWriting(moveCursorToCaret: true);
        }
    }

    private void StartWriting()
    {
        NativeMethods.GetCursorPos(out var cursor);

        // If the mouse has not moved since writing stopped, continue the same line.
        if (cursor != _resumeCursor)
        {
            var start = Board.PointFromScreen(new Point(cursor.X, cursor.Y));
            var halfLine = _writer.LineHeight / 2;
            start = new Point(
                Math.Clamp(start.X, 0, Math.Max(0, Board.ActualWidth)),
                Math.Clamp(start.Y, halfLine, Math.Max(halfLine, Board.ActualHeight - halfLine)));

            _writer.StartLine(start);
            _currentLineStrokes.Clear();
        }

        // Lock the cursor in place, so that the touchpad does not move it while writing.
        var clip = new NativeRect { Left = cursor.X, Top = cursor.Y, Right = cursor.X + 1, Bottom = cursor.Y + 1 };
        NativeMethods.ClipCursor(ref clip);
        Mouse.OverrideCursor = Cursors.None;

        _isWriting = true;
        UpdateOverlay();
        UpdateStatus();
    }

    private void StopWriting(bool moveCursorToCaret)
    {
        if (!_isWriting)
        {
            return;
        }

        FinishActiveStroke();
        _isWriting = false;
        NativeMethods.ReleaseCursorClip(IntPtr.Zero);
        Mouse.OverrideCursor = null;

        if (moveCursorToCaret)
        {
            var caret = Board.PointToScreen(new Point(_writer.CaretX, _writer.LineTop + (_writer.LineHeight / 2)));
            NativeMethods.SetCursorPos((int)Math.Round(caret.X), (int)Math.Round(caret.Y));
        }

        NativeMethods.GetCursorPos(out var cursor);
        _resumeCursor = cursor;

        UpdateOverlay();
        UpdateStatus();
    }

    private void OnFingerDown(TouchpadPoint finger)
    {
        if (!_isWriting)
        {
            return;
        }

        _writer.PadAspectRatio = _touchpad.AspectRatio;
        _smoother.Reset();
        var start = _smoother.Smooth(_writer.BeginStroke(finger, Now));

        _activeStroke = new Stroke(new StylusPointCollection([new StylusPoint(start.X, start.Y)]), CreatePen());
        Board.Strokes.Add(_activeStroke);

        UpdateOverlay();
        UpdateStatus();
    }

    private void OnFingerMoved(TouchpadPoint finger)
    {
        if (_activeStroke is null)
        {
            return;
        }

        var point = _smoother.Smooth(_writer.ContinueStroke(finger));
        var last = _activeStroke.StylusPoints[^1];
        var distance = Math.Abs(point.X - last.X) + Math.Abs(point.Y - last.Y);
        if (distance < MinPointSpacing)
        {
            return;
        }

        _activeStroke.StylusPoints.Add(new StylusPoint(point.X, point.Y));
    }

    private void FinishActiveStroke()
    {
        if (_activeStroke is null)
        {
            return;
        }

        var points = _activeStroke.StylusPoints;
        var shift = _writer.EndStroke(points.Min(point => point.X), points.Max(point => point.X), Now);
        if (shift > 0)
        {
            _activeStroke.Transform(new Matrix(1, 0, 0, 1, shift, 0), applyToStylusTip: false);
        }

        _currentLineStrokes.Add(_activeStroke);
        _activeStroke = null;

        UpdateOverlay();
    }

    private void UndoLastStroke()
    {
        if (_activeStroke is not null || Board.Strokes.Count == 0)
        {
            return;
        }

        var lastStroke = Board.Strokes[^1];
        Board.Strokes.Remove(lastStroke);

        if (_currentLineStrokes.Remove(lastStroke))
        {
            _writer.SetInkRight(_currentLineStrokes.Count == 0 ? null : _currentLineStrokes.Max(StrokeRight));
        }
    }

    private void ClearBoard()
    {
        if (_activeStroke is not null)
        {
            return;
        }

        Board.Strokes.Clear();
        _currentLineStrokes.Clear();
        _writer.SetInkRight(null);
    }

    private static double StrokeRight(Stroke stroke) => stroke.StylusPoints.Max(point => point.X);

    private static TimeSpan Max(TimeSpan first, TimeSpan second) => first > second ? first : second;

    private static TimeSpan Min(TimeSpan first, TimeSpan second) => first < second ? first : second;

    private DrawingAttributes CreatePen() => new()
    {
        Color = Colors.Black,
        Width = _penThickness,
        Height = _penThickness,
        StylusTip = StylusTip.Ellipse,
        FitToCurve = true,
        IgnorePressure = true,
    };

    private void UpdateOverlay()
    {
        var window = _writer.WritingWindow;
        WritingWindowFrame.Visibility = _activeStroke is null ? Visibility.Collapsed : Visibility.Visible;
        WritingWindowFrame.Width = window.Width;
        WritingWindowFrame.Height = window.Height;
        System.Windows.Controls.Canvas.SetLeft(WritingWindowFrame, window.Left);
        System.Windows.Controls.Canvas.SetTop(WritingWindowFrame, window.Top);

        Caret.Visibility = _isWriting && _activeStroke is null ? Visibility.Visible : Visibility.Collapsed;
        Caret.X1 = _writer.CaretX;
        Caret.X2 = _writer.CaretX;
        Caret.Y1 = _writer.LineTop;
        Caret.Y2 = _writer.LineTop + _writer.LineHeight;
    }

    private void UpdateStatus()
    {
        var mode = _isWriting ? "WRITING (Caps Lock on)" : "Mouse (point, then Caps Lock to write)";
        var touchpad = _touchpad.HasSeenTouchpad ? "found" : "touch it to check";
        var lastLift = _writer.LastOffPadTime is { } offPad
            ? $"{offPad.TotalSeconds:0.00} s ({(_writer.LastStrokeMovedAlong ? "moved along" : "same word")})"
            : "-";

        StatusText.Text =
            $"{mode}   |   Touchpad: {touchpad}   |   Pen [ ]: {_penThickness}   |   Line height - +: {_writer.LineHeight}   |   " +
            $"Letter gap , .: {_writer.LetterGapRatio:P0}   |   " +
            $"Smoothing S: {_smoother.Level}   |   Move along A: {_writer.AdvanceMode}   |   " +
            $"Pause ; ': {_writer.PauseBeforeMoveAlong.TotalSeconds:0.0} s   |   Last lift: {lastLift}   |   " +
            "Space, Enter, Backspace = undo stroke, Delete = clear";
    }
}
