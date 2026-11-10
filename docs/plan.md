# Trackpad Whiteboard, phase 1 — Plan

> **What this document is for:** It answers *how will we build it?* It turns the approved requirements in spec.md into a design, and it is agreed before any code is written.

Status: Approved
Last updated: 2026-11-10

---

## 1. Approach

Grow the spike into the real app, in the same .NET 10 WPF project. First make the writing testable and improve it until it passes the writing test (spec section 6.2.1), as the build order in spec section 3 requires. Then add the board, saving and the home screen around it. Placement logic stays pure, with no WPF controls, so that automated tests and recorded handwriting can prove it.

### 1.1 Options considered

| Option | Pros | Cons |
| --- | --- | --- |
| Grow from the spike | The input code already works on the owner's touchpad, with strict build settings. The code is small. The owner can test the writing from the first day. | `MainWindow.xaml.cs` holds too much and must be split. |
| Start clean and copy parts | A tidy start. | The copied parts are the same files. The owner has nothing to test until the new shell works. |
| Another UI framework, such as WinUI | Newer ink controls. | Rewrites working code, and solves no problem in the spec. The probe in section 9 shows that WPF ink is fast enough. |

Chosen: grow from the spike. It keeps what the owner already tested, and it does not delay the writing work.

---

## 2. Configuration

The app has no secrets and no `appsettings.json`.

| Setting | Stored in | Default | Notes |
| --- | --- | --- | --- |
| Line height on the screen | `settings.json` | 90 | Value from the spike. Spec section 6.1.8 |
| Pen thickness on the screen | `settings.json` | 6 | Value from the spike. Spec section 2.4 |
| Letter gap, word gap, smoothing, pause and jump values | Constants in code | Spike values | Tuned with the tuning keys, then fixed (spec section 6.1.8) |
| Data folder | Constant in code | `%LOCALAPPDATA%\TrackpadWhiteboard` | Local only (spec section 6.1.7). The stores take the folder in their constructor, so tests can give a temporary folder. |

---

## 3. Architecture

### 3.1 Components

One app project and one test project. A separate logic project gives no benefit yet, because `LineWriter` uses WPF geometry types, so the tests need the Windows target in any case.

```text
src/TrackpadWhiteboard.App/
  App.xaml(.cs)        Start-up, one running copy, global error handling
  MainWindow           Holds the current screen and the window message hook
  Input/               TouchpadReader, TouchpadDevice, ContactTracker, TouchpadDetector, TouchpadRecorder, KeyMap
  Interop/             Windows API declarations (no change)
  Writing/             LineWriter, PointSmoother, word boundary methods
  Boards/              Board, BoardDetails, BoardSnapshot, BoardStore, BoardLayout, UndoHistory, BoardSaver
  Screens/             HomeScreen, BoardScreen, SettingsView
  Settings/            AppSettings and its store
tests/TrackpadWhiteboard.App.Tests/
  Writing/, Boards/, Input/    MSTest tests
  TestData/Recordings/         Recordings of the test lines only (section 6)
```

### 3.2 Existing code to follow or reuse

* `TouchpadReader.cs` and `TouchpadDevice.cs` — keep. Move the frame logic in `TouchpadReader.cs:114-162` (`ReadReport` and `HandleContact`) into `ContactTracker`, which takes parsed contacts and has no Windows calls, so tests can feed it frames. Add the lost finger-up fix in section 4 there.
* `LineWriter.cs` — keep as the base for placement. It already takes the time as an argument and has no WPF controls, so it is easy to test.
* `PointSmoother.cs` — keep.
* `MainWindow.xaml.cs:98-127` — the tuning keys. Move them out, then remove them as spec section 6.1.8 requires.
* `MainWindow.xaml.cs:85-97` — Space, Enter, Backspace and Delete. Move them into `BoardScreen`, through `KeyMap`.
* `MainWindow.xaml.cs:73-79` and `137-200` — the Caps Lock and cursor lock flow (`OnPreviewKeyUp`, `SyncWritingModeWithCapsLock`, `StartWriting` and `StopWriting`). Keep it, and move it into `BoardScreen`.
* `MainWindow.xaml.cs:60` — the result of `TouchpadReader.Register` is ignored. Handle it as section 4 says.

### 3.3 Interfaces and contracts

`LineWriter` decides the gap, and it returns moves for the screen to apply. It does not touch WPF strokes.

```csharp
internal enum WordBoundaryMethod { AppWorksItOut, RowRule, LongerPause, TouchpadSignal }

internal enum GapKind { None, Letter, Word }

// What the screen must do when the finger lifts.
internal sealed record StrokePlacement(double ShiftX, WordMove? MoveToNewLine);

internal sealed record WordMove(IReadOnlyList<int> StrokeIds, double OffsetX, double OffsetY);

internal sealed class LineWriter
{
    public Point BeginStroke(TouchpadPoint finger, TimeSpan now);
    public Point ContinueStroke(TouchpadPoint finger);
    // The bounds give the stroke's size, so a wrong guess at finger down can be corrected.
    public StrokePlacement EndStroke(int strokeId, Rect strokeBounds, TimeSpan now);
    public void StartLine(Point start, Rect section, double lineHeight);
    public void NewLine();
    public void AddWordGap();
    public void RemoveStroke(int strokeId);
    public void RestoreStroke(int strokeId, Rect strokeBounds);
}

// The board as it was at one moment. It is made on the UI thread and saved on a background thread.
internal sealed record BoardSnapshot(BoardDetails Details, StrokeCollection Strokes);

internal sealed class BoardStore
{
    public BoardStore(string dataFolder);
    public IReadOnlyList<BoardSummary> ListBoards();
    public Board Open(Guid id);
    public Board Create(BoardLayoutKind layout, Size screen);
    public void Rename(Guid id, string name);
    public void Delete(Guid id);
    public void Save(BoardSnapshot snapshot);   // Runs on a background thread.
    public void SavePreview(Guid id, BitmapSource preview);
}

// Decides when to save. See "Saving" in section 3.5.
internal sealed class BoardSaver
{
    public BoardSaver(BoardStore store, Func<BoardSnapshot> takeSnapshot, TimeProvider time);
    public void MarkChanged();
    public Task SaveNowAsync();
}

// The frame logic from TouchpadReader, with no Windows calls.
internal sealed class ContactTracker
{
    public event Action<TouchpadPoint>? FingerDown;
    public event Action<TouchpadPoint>? FingerMoved;
    public event Action? FingerUp;
    public void ReadReport(int? contactCount, IReadOnlyList<TouchpadContact> contacts);
}

internal static class TouchpadDetector
{
    // Each item is the usage page and usage of one Raw Input device.
    public static bool HasPrecisionTouchpad(IEnumerable<(ushort UsagePage, ushort Usage)> devices);
}

internal enum BoardAction { NewLine, WordGap, Undo, Redo, PenBlack, PenBlue, PenRed, PenGreen, Eraser, Clear, ZoomIn, ZoomOut, MoveView, WholeBoard, Home }

internal static class KeyMap
{
    // Null means that the key does nothing in that mode.
    public static BoardAction? ActionFor(Key key, bool isWriting);
}
```

* `BoardDetails` holds the values in `board.json` (section 3.4).
* `TouchpadDevice` reads the HID reports. `TouchpadReader` passes the parsed contacts to `ContactTracker`.
* Each stroke has an ID from a counter. It is given when the stroke is drawn or the board opens, and kept as custom stroke data (`Stroke.AddPropertyData`), so a moved copy keeps it. IDs are not used after a board is opened again.

Keys:

| Key | Mode | Action | Spec |
| --- | --- | --- | --- |
| Enter, Space | Writing | New line, word gap | 6.1.2 |
| Backspace | Both | Undo | 6.1.6 |
| `Y` | Both | Redo | 6.1.6 |
| `1` to `4` | Both | Black, blue, red, green | 6.1.6 |
| `E` | Mouse | Eraser on or off | 6.1.6 |
| Delete | Mouse | Clear the board | 6.1.6 |
| `+`, `-`, arrow keys | Mouse | Zoom, move the view | 6.1.3 |
| `0` | Mouse | See the whole board | 6.1.3 |
| Esc | Mouse | Go back to the home screen | 6.4.2 |

### 3.4 Data model

Each board has its own folder:

```text
%LOCALAPPDATA%\TrackpadWhiteboard\
  settings.json
  Boards\<board id>\board.json     Details of the board
  Boards\<board id>\strokes.isf    The ink
  Boards\<board id>\preview.png    The picture for the home screen
  Recordings\                      Raw touchpad recordings
  Logs\app.log
```

`board.json`:

```json
{
  "schemaVersion": 1,
  "id": "6f1c2d3e-0000-4000-8000-000000000000",
  "name": "2026-11-06 21:04",
  "layout": "ThreeColumns",
  "width": 7680,
  "height": 4320,
  "titleBoxHeight": 351,
  "lastChangedUtc": "2026-11-06T21:04:00Z",
  "view": { "zoom": 1.0, "offsetX": 0, "offsetY": 0 },
  "penColour": "Black"
}
```

* All sizes are WPF device-independent units on the board.
* `width` and `height` are 4 times the primary screen size when the board is created (spec section 6.1.3).
* `titleBoxHeight` is 3 lines at the line height and line spacing in use when the board is created (spec section 6.1.5).
* Line details are kept in memory only. After a board opens, writing starts on a new line (spec section 6.1.1), so saved boards do not need them.
* The undo history is kept in memory only, for the time a board is open, as spec section 6.6 allows.

A recording is one JSON object per line, with the raw values:

```json
{"t":1234.5,"e":"down","x":0.12,"y":0.51,"id":3,"fingers":1}
```

Enter and Space presses are recorded too, so that the replay starts lines and word gaps where the owner did:

```json
{"t":1290.0,"e":"key","key":"Enter"}
```

### 3.5 Key decisions

| Decision | Choice | Why | Alternatives rejected |
| --- | --- | --- | --- |
| Word boundary methods | One `WordBoundaryMethod` setting while testing. Only the winner stays. | Spec section 6.1.2.1 needs the methods compared on the same recordings. The spike's `AdvanceMode` already works this way. | An interface with one class per method: too much for code that is removed later. |
| Moving along and the gap | Two separate settings while testing. The spike's `AdvanceMode` decides whether a stroke starts a new pass or adds to the letters already there. `WordBoundaryMethod` decides the gap after a new pass: a letter gap or a word gap. After the writing test, only the winning value of each stays (spec section 6.1.8 item 2). | They answer different questions, and the dots and crossbars work changes only the first. | One setting for both: each change would mix the two kinds of error. |
| Finding the word boundary method | Record first, then replay the recordings through `LineWriter` in tests. | Same input for every method, and no need for the owner to rewrite lines for each change. | Only live testing: slow and hard to compare. |
| Labelling recordings | After recording, from a picture of numbered strokes. | Pressing a key while writing would change the timing that the app must read. | A label key while writing. |
| Row rule mapping | The row is set at finger down. Y maps from that row to the line height, and X maps across a window 3 times as wide. A stroke that leaves its row keeps its row's mapping. | It matches spec section 6.1.2.1, and letters are not cut off. | Changing the row during a stroke: a letter would jump. |
| Touchpad signal | If it is needed, a tap with a second finger. | A one-finger tap looks the same as an "i" dot. | A one-finger tap. |
| Dots and crossbars | Guess at finger down: first test, a stroke that starts inside the current word's ink stays on its letter. At lift, the stroke's size can correct a wrong guess, and the stroke then jumps once. Chosen by the owner on 10 November 2026: a rare jump is accepted. The replay counts the jumps. Prove or replace the first test with recordings. | Defect 2 in spec section 4.1. The size is known only when the finger lifts, and ink must show within 50 ms (spec section 6.5 item 1). | Treat every pause as moving along: this is the defect. Hold the ink until lift: too slow. |
| Ink store | Ink Serialized Format through `StrokeCollection.Save`. | Built in. The probe confirms that colour, thickness and custom data survive. | Own JSON format: more code, and larger files. |
| Details store | `System.Text.Json` | The standard for .NET projects under the owner's rules. | A database: not needed for one user and one folder per board. |
| Zoom and view | A `MatrixTransform` on the board's layer. | Ink stays vector, so it stays sharp (spec section 6.1.3). The probe shows it is smooth. | A control that handles zoom for us: nothing needs it. |
| Same size on the screen | New strokes get thickness and line height divided by the zoom. | Spec section 6.1.3 item 7. | Scaling the strokes later: changes saved ink. |
| Pinch and scroll | Read `WM_MOUSEWHEEL` and `WM_MOUSEHWHEEL` in the existing window hook, in mouse mode only, and use the size of each delta. A wheel message with `MK_CONTROL` set in `wParam` is a pinch, and zooms. Any other is a scroll, and moves the view. | Microsoft documents that default touchpad handling typically generates mouse wheel messages, and that touchpads send very small deltas. For touch screens, Microsoft documents that a zoom gesture arrives as `WM_MOUSEWHEEL` with `MK_CONTROL`. This is not documented for touchpads, so it is checked first (section 9). The hook already exists and gives the raw values. | Registering as a touchpad-capable window: more code, with no benefit yet. |
| Saving | Save from a background thread, from a snapshot, at most every 3 seconds. See the saving steps below this table. | Spec section 6.5 items 3 and 4. The probe shows 404 ms on the UI thread for 20,000 strokes, which would delay writing. | Saving on the UI thread. Waiting for a quiet moment (debounce): continuous writing would never save. |
| Preview picture | Made on the UI thread when the owner leaves the board or closes the app, at 320 units wide, and saved with the last save. | `RenderTargetBitmap` needs the UI thread. Leaving is the one time when a short wait does not delay writing. | A preview with each save: it would delay writing. |
| Moves during saving | A move replaces a stroke with a moved copy. It does not change a stroke that a snapshot can hold. | The background save reads strokes with no lock. | Locks on the UI thread: they could delay writing. |
| Clear the board | Delete clears at once, and one undo brings it back. | Spec section 6.1.6 item 7 allows this. It needs fewer key presses (spec section 6.5 item 7). | Asking first. |
| Lines after undo and redo | `LineWriter` keeps each line's stroke IDs and bounds. `RemoveStroke` and `RestoreStroke` update that line, and the caret goes to the end of its ink. After a clear is undone, writing starts on a new line below the ink, as when a board opens. | Spec section 6.1.6 items 6, 7 and 9, and defect 10 in spec section 4.1. | Rebuilding the lines from the strokes' positions: rows can overlap, so the line is a guess. |
| Clicks in writing mode | The board ignores mouse clicks in writing mode: the ink layer's editing mode is `None` and it does not take mouse input. The eraser comes back when mouse mode starts. | Spec section 6.1.2 item 2 and section 6.6 (tap-to-click). The spike's `InkCanvas` (`MainWindow.xaml:11`) would erase a stroke on a tap while the eraser is on. | Turning the eraser off in writing mode: the owner would have to turn it on again. |
| Finding the touchpad | At start-up, `GetRawInputDeviceList` and `GetRawInputDeviceInfo` list the Raw Input devices. `TouchpadDetector` looks for usage page `0x0D`, usage `0x05`. | Spec section 6.6 needs the message when the owner tries to write, and `HasSeenTouchpad` is true only after a touch. Microsoft documents that each HID device gives its usage page and usage. | Waiting for the first touch: with no touchpad, nothing ever comes. |
| Screens | One window, with `HomeScreen` and `BoardScreen` as user controls, and code-behind. | It matches the spike. The logic is in plain classes, so a view model pattern adds no value. | An MVVM framework. |
| Start-up and services | No Generic Host and no dependency injection container. | The rules ask for the Host for long-running apps such as workers and services. This is a small desktop app. | The Generic Host. |
| One running copy | A named `Mutex`, and the first copy comes to the front. | Spec section 6.6. | Allowing several copies: they could overwrite a board. |
| Grown board | `height` grows by one screen when a new line is below the bottom. | Spec section 6.1.4 item 4. | Growing by one line: too many layout changes. |

Saving steps:

1. Every change to the board calls `BoardSaver.MarkChanged`. A change is a stroke that ends, an erase, an undo, a redo, a clear, a word move, a grown board, a new view or a new pen colour.
2. If no save is waiting, `MarkChanged` starts a 3-second timer from `TimeProvider`. More changes during those 3 seconds do not restart it.
3. When the timer ends, the UI thread makes a `BoardSnapshot`. It copies the list of strokes, not the strokes, and leaves out the stroke being drawn. That stroke is in the next save.
4. A background thread runs `BoardStore.Save`. It writes `strokes.isf` and then `board.json`, each to a `.tmp` name first.
5. A change during a save starts a new timer. If that timer ends while the save still runs, the next save starts when the running save ends. Only one save runs at a time. So no more than 3 seconds plus one save is lost (spec section 6.5 item 4).
6. When the window stops being active, for example when the laptop sleeps, the app saves at once, with no preview (spec section 6.6).
7. When the owner leaves the board or closes the app, the app stops the timer, waits for a running save, then saves at once and saves the preview.
8. If that last save fails, the app shows the message from section 4 and stays on the board, as spec section 6.6 requires. Nothing is lost while the board stays open.
9. Rename and delete happen only on the home screen. No board is open there, so they never run during a save.

### 3.6 Design direction

The owner chose the character, the reference and the signature, and approved the mock-ups on 9 November 2026. The owner still signs off the look of each real screen.

**Mock-ups:** `docs/design/` holds the four approved screens: `home.dc.html`, `board-mouse-mode.dc.html`, `board-writing-mode.dc.html` and `three-columns.dc.html`. They are the source files of the owner's private design canvas, and they render only there. The screens are built to them. When a mock-up and the spec disagree, the spec wins. The ink in them is a handwriting font with the writing-test lines, and the board names are sample values. The fifth file, `touchpad-outline-steps.dc.html`, shows in 8 steps how the touchpad outline moves while the owner writes. Its letter and word gaps are approximate.

| Item | Proposal |
| --- | --- |
| References | Chosen by the owner: Microsoft Whiteboard. Take its ideas, not its look: a light board with a lot of space, one small floating pen tray, and slim controls at the edges. The owner wants the app to feel unique and fresh. |
| Tools | Approved in the mock-ups: one floating tray at the bottom centre in mouse mode, with Write, the four pens, eraser, undo, redo, zoom out, the zoom level, zoom in, whole board and home. Each button shows its key, so the tray also teaches the keys. Write is a label, not a button: it shows the Caps Lock key and does nothing when clicked, because the app does not change Caps Lock (spec section 5.2) and a click would move the pointer to the tray. Clear the board is on the Delete key only. In writing mode the tray hides, and a small "Writing" sign with the pen colour takes its place. Nothing sits in the top-left corner, where a new board starts. |
| Character | Chosen by the owner: quiet and plain, like a clean whiteboard in a room. The ink is the main thing on the screen. |
| Signature | Chosen by the owner: the touchpad outline. In writing mode, a faint rounded frame on the board shows the writing window, so the owner sees where the next stroke lands (spec section 6.1.1 item 6). It works like a large text cursor, not a mouse pointer: it stays still while a pass is written, and moves only when the writing window moves along (a new stroke starts on the left, Enter, or a right edge). It is drawn on a layer above the ink that is not saved. |
| Type | Segoe UI, because it is already on Windows. Sizes 11 (key labels), 14 (body and buttons), 18 (board names) and 28 (home screen title). Weights 400 and 600. |
| Colour | App background `#F3F3F1`. Board, cards and tray `#FFFFFF`. Text `#1F1F1F`. Secondary text `#5C5C5C`. Borders `#E1E1DE`, button border `#D6D6D2`, key labels and board dividing lines `#CFCFCB`. Selected tool `#EDEDEA`. Primary button `#1F1F1F` with white text. Ink only: black `#1A1A1A`, blue `#1E5BD8`, red `#D2322D`, green `#1E8A3E`. Touchpad outline and caret `#5A6E8C` at 45% opacity, with a fill of `#5A6E8C` at 4%. Row labels in the row rule outline `#3F4E66`. Preview area on a home card `#F7F7F5`. `#9A9A96` and the blue finger mark in `touchpad-outline-steps.dc.html` only explain the outline, and are not in the app. |
| Sizes | Tray buttons 56 × 64, other buttons at least 44 high. Corner radius 4 for key labels, 6 for the items in the "Writing" sign, 8 for small icon buttons, 10 for buttons, 12 for cards, 16 for the tray and 18 for the touchpad outline. The tray has a soft shadow: 6 down, 20 blur, black at 8%. Board dividing lines 2 wide. |
| Empty home screen | No mock-up. It follows spec section 6.4.1: the new board action and one short sentence, in the same type and colours as `home.dc.html`. The owner signs off its look on the real screen. |
| Access | Each button has an automation name, such as "Undo" or "Open board", and a key focus order from left to right. |
| Motion | None while writing. Chosen by the owner on 9 November 2026: the touchpad outline and the caret jump to their new place, with no slide. A short zoom to a board when it opens, if it causes no delay. |
| Avoid | Full-width toolbars, dialogs, dark or decorated backgrounds, dots or grids on the board, and copying Microsoft Whiteboard's look. |
| Content | The owner's own boards. |

---

## 4. Error handling

| Situation | Behaviour |
| --- | --- |
| No precision touchpad | When Caps Lock turns on and `TouchpadDetector` found no touchpad, writing mode does not start. The board screen shows: "No precision touchpad was found. Trackpad Whiteboard needs one to write." (spec section 6.6). Devices used through Remote Desktop are not in the device list, so the message also shows there. |
| Raw Input registration fails | `TouchpadReader.Register` returns false. Log it, and show the same message. |
| A finger-up report is lost | When a complete frame does not hold the tracked contact, end the stroke. The next touch starts a new stroke (spec section 6.6). |
| Writing a save fails | Keep the board in memory, show a message, try again at the next save, and log it. The old files stay in place. |
| The app stops during a save | Files are written to a `.tmp` name and replace the old file only after they are complete. So the last complete save stays usable. |
| `board.json` or `strokes.isf` cannot be read | The home screen shows the board as damaged (spec section 6.4.1). The other boards work. |
| `preview.png` is missing or damaged | Show a blank preview. The board still opens. |
| `settings.json` cannot be read | Use the defaults and log it. |
| A new name is empty or only spaces | Keep the old name. Names are trimmed and cut to 100 characters. |
| Rename or delete fails, for example a file is in use | Show a message, keep the list as it was, and log it. A delete first renames the board's folder to a `.deleted` name, then removes it. If the rename fails, the board stays whole. A `.deleted` folder left behind is removed at the next start, and the home screen does not list it. |
| Unexpected exception | One handler in `App.xaml.cs` logs it, releases the cursor lock, tries a last save and shows a message. |
| Window stops being active | The existing `Deactivated` flow ends the stroke and frees the cursor (spec section 6.1.1). |
| The laptop sleeps or locks | The same flow must run (spec section 6.6). Not yet tested: check on the laptop, and also handle the Windows session events if the flow does not run. |
| App closes in writing mode | Release the cursor lock and save first. |

---

## 5. Observability

* `Logs\app.log` holds errors and warnings, through `ILogger<T>` with a small file logger. It never holds ink or board names.
* When `app.log` is larger than 1 MB at start-up, it becomes `app.old.log`, and any older `app.old.log` is removed. So the logs never use more than about 2 MB.
* While the tuning keys exist, the status bar shows the time from the touchpad report to the next rendered frame, plus the move-along decision and its reason. This gives the numbers for spec section 6.5 item 1.
* After that, the app logs the slowest writing response in each session, and each save that takes longer than 1 second.

---

## 6. Security

| Item | Decision |
| --- | --- |
| Network | None. The app has no network code (spec section 6.5 item 8). |
| Secrets | None. |
| Data | Boards, settings and recordings stay in the current user's local app data folder. |
| Files read | Only from the app's own folder. A damaged file is handled as section 4 says. |
| Recordings | They hold the owner's handwriting, and the repository is public. The owner approved recordings of the test lines in spec section 6.2.1 in `tests/TrackpadWhiteboard.App.Tests/TestData/Recordings/`. Recordings of real notes stay in the local `Recordings\` folder and never go in the repository. |
| Logs | No ink, names or personal data. |

---

## 7. Testing

### 7.1 Principles

* MSTest, in `tests/TrackpadWhiteboard.App.Tests`, with internals visible to the test project. The project uses the Windows target, because the app uses WPF types.
* Test placement through `LineWriter`'s public methods, with data-driven tests for the worked examples in spec section 6.2.
* Replay recorded handwriting through `LineWriter`, and check the result against labels. This compares the methods in spec section 6.1.2.1.
* Store tests use a new temporary folder for each test, passed to the store's constructor.
* Seams for code that touches Windows: `ContactTracker` takes parsed contacts, `TouchpadDetector` takes a device list, `KeyMap` maps keys with no window, and `BoardSaver` takes a `TimeProvider`. Saver tests use `FakeTimeProvider` from the `Microsoft.Extensions.TimeProvider.Testing` package.
* The feel of writing, pinch gestures and the writing test need the owner on the real touchpad. Automated tests do not replace these.

### 7.2 Tests

| # | Test | Spec | Defect it catches |
| --- | --- | --- | --- |
| 1 | Each row of the worked examples, as finger events through `LineWriter` | 6.2 | Any placement rule breaks |
| 2 | A jump in a word gives a letter gap, and a new word gives a word gap, for each method | 6.1.2.1 | Split words, joined words |
| 3 | Replay recordings against labels, and report errors for each method | 6.1.2.1, 6.2.1 | A method that looks good only by hand |
| 4 | Row rule: lower row continues, same or higher row starts a new word | 6.1.2.1 | The left-middle conflict |
| 5 | Dot, crossbar and bowl strokes stay on their letter | 6.1.2 item 10 | Defects 2, 3 and 4 in section 4.1 of the spec |
| 6 | Two short words do not overlap | 6.1.2 item 7 | Short words stack |
| 7 | Slow writing does not spread letters | 6.1.2 item 8 | "L a z y" |
| 8 | A lost finger-up ends the stroke, and the next touch starts a new one | 6.6 | Defect 6 in section 4.1 of the spec |
| 9 | A second finger has no effect on a stroke | 6.1.2 item 2 | Stray ink |
| 10 | At a right edge, the word's stroke IDs and offsets are correct | 6.1.4 | Word left behind |
| 11 | A word wider than the section splits at the edge | 6.1.4 | Endless move loop |
| 12 | A new line below the bottom asks for a taller board | 6.1.4 | Writing goes off the board |
| 13 | Line height and thickness are divided by the zoom | 6.1.3 item 7 | Wrong size when zoomed |
| 14 | Three-column layout gives equal columns and the fixed title box height, also after the board grows | 6.1.5 | Title box changes size |
| 15 | Undo, redo, erase and clear in order, with the caret back on the right line | 6.1.6, 6.2 | Defects 9 and 10 in section 4.1 of the spec |
| 16 | Save then open gives the same strokes, colours, layout, name, view and pen colour | 6.1.7 | Lost data |
| 17 | A failed save leaves the old files readable | 6.6 | A damaged board |
| 18 | A damaged board lists as damaged, and the others still list | 6.4.1, 6.6 | The home screen stops working |
| 19 | The list is in order of most recent change, and delete and rename work | 6.4.1 | Wrong board shown |
| 20 | Saves come at most every 3 seconds, one at a time, and leave out the stroke being drawn | 6.1.7, 6.5 items 3–4 | Lost writing, or saves that pile up |
| 21 | `TouchpadDetector` finds a precision touchpad in a device list, and finds none in a list without one | 6.6 | No message without a touchpad |
| 22 | Each key in section 3.3 gives its action in the right mode. After the tuning keys are removed, letter and punctuation keys give none. | 6.1.8 item 4, 6.5 item 7 | A stray key changes writing |
| 23 | A `settings.json` that cannot be read gives the defaults | 6.1.8 item 3 | The app does not start |
| 24 | With the eraser on, a click in writing mode erases nothing | 6.6 | A tap erases a stroke |
| 25 | The app's assembly references no `System.Net.Http` or `System.Net.Sockets` types | 6.5 item 8 | A network connection added by mistake |

Owner checks, on the real touchpad:

1. The writing test in spec section 6.2.1, first when the writing is done and again after zoom and saving are in, because writing wins (spec section 3).
2. Pinch, two-finger scroll and every key in section 3.3.
3. Five working days of the owner's own notes (spec section 3).

---

## 8. Infrastructure and deployment

The app has no hosted resources, and it uses no Azure services and has no cost.

* **CI:** one GitHub Actions workflow on `windows-latest` runs `dotnet build` and `dotnet test` on each pull request and on `main`. The repository is public, and GitHub's billing documentation says that standard hosted runners are free in public repositories.
* **Branch protection:** the ruleset "Protect main" has been active since 9 November 2026, at the owner's request. It requires a pull request with 0 approvals, blocks force pushes and deletion, and has no bypass. After the CI workflow has run once, add its build and test job as a required status check, with the owner's approval, because it changes a repository setting.
* **Install:** `dotnet publish` to a local folder, with a desktop shortcut. It needs the .NET 10 runtime, which is supported until November 2028.
* **Roll back:** each publish goes to a new folder named by its date. To go back, point the shortcut at the previous folder. The boards stay in the data folder, and phase 1 has only `schemaVersion` 1, so an older build can open them.
* **Build settings:** add `Directory.Packages.props` for package versions, and add the test project to `TrackpadWhiteboard.slnx`. Add `global.json` to pin the .NET 10 SDK, with roll-forward to the latest feature band. The CI workflow reads it through `actions/setup-dotnet` and its `global-json-file` input, so the laptop and CI use the same SDK.

---

## 9. Risks

Probe results come from a throwaway WPF program, run on the owner's laptop on 6 November 2026. It used 20,000 synthetic strokes of 60 points each.

| Measure | Result |
| --- | --- |
| Save to memory on the UI thread | 404 ms, 3 MB |
| Save on a background thread | Works, 187 ms |
| Load the ink | 338 ms |
| Assign the ink and reach the first idle moment | 1,944 ms |
| Frame time while adding points on the full board | Median 11 ms, 95% within 26 ms, slowest 37 ms |
| Frame time while zooming the full board | Median 9 ms, 95% within 16 ms, slowest 35 ms |
| Save and load | Colour, thickness, smoothing and custom data survive. Thickness 1.5 returned as 1.49998, and the one point checked moved by 0.006 units. |

| Risk | Likelihood | Mitigation |
| --- | --- | --- |
| No method in spec section 6.1.2.1 passes the writing test | Medium | Record first, compare with replay, and stop and report. Spec section 8 says that the spec then changes. |
| The row rule makes letters wavy or shaky | Medium | Try it in a short session before fully building it. Keep the method switch until the writing test passes. |
| Opening a board with 20,000 strokes takes about 2.3 seconds, close to the 3 seconds in spec section 6.5 item 5 | Medium at that size | Measure with real boards. Next option if it is slow: draw finished ink on fewer visuals. |
| Pinch does not arrive as wheel messages on the owner's touchpad | Low to medium | Test early. The `+` and `-` keys and arrow keys already work. |
| The probe does not match real use, such as more points in a stroke | Medium | Measure with real boards. Next option if it is slow: draw finished ink on fewer visuals. |
| The 50 ms in spec section 6.5 item 1 cannot be measured up to the screen itself | Certain | Measure from the touchpad report to the next rendered frame. The display's own delay is not included. The owner's check is the final judge. |
| Word moves at a right edge are distracting | Medium | The owner tries them during the 5-day use. |
| The touchpad outline distracts, or its moves along feel jumpy | Medium | The owner judges it in the writing test. Make it fainter, or show it only while no finger touches. |
| Layout code and word moves make `LineWriter` too large | Medium | Split it when a real boundary appears, with tests already in place. |
| Three- and four-finger gestures, such as a swipe to switch apps, still reach Windows while the owner writes | Low | The app follows one finger only, so a stray swipe is rare. The owner checks it in the writing test. If it happens, the owner can turn those gestures off in Windows settings. Microsoft documents `TouchpadGesturesController` for apps that take these gestures, which is the next option. |

### Open questions

| # | Question | Recommendation |
| --- | --- | --- |
| Q1 | Design direction in section 3.6. | Resolved: quiet and plain, Microsoft Whiteboard's ideas as the reference, and the touchpad outline as the signature. |
| Q2 | Opening a very large board took longer than the 2 seconds that spec section 6.5 item 5 allowed. | Resolved: the owner changed it to 3 seconds at 20,000 strokes, and the app shows that the board is opening. |
| Q3 | Can recordings of the owner's handwriting go in the public repository? | Resolved: yes for the test lines in spec section 6.2.1, which hold no private content. Not for real notes. |
