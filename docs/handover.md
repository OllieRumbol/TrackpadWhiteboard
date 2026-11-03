# Handover: from the spike to the real app

This file carries the context of the spike session (2 November 2026) into the next session. Read `README.md` first: it has the keys and the tuning findings. This file adds what the README does not hold.

When `docs/spec.md` exists, move what is still useful from this file into the spec, then delete this file.

## Why this app exists

The owner cannot grip a pen. At school, the one place where writing was easy was a whiteboard on a wall, with a big fat pen. They have not written by hand for many years and miss the freedom of it: putting notes down quickly, using lots of space. Microsoft Whiteboard is no longer an option for them.

Every design decision serves one goal: **writing must feel free and easy**. Any extra key press, wait or correction works against that goal.

## The original vision

In the owner's words, summarised:

1. A large blank canvas on Windows.
2. Point the mouse where you want to start writing.
3. Turn Caps Lock on. The cursor freezes where it is.
4. Write with one finger on the trackpad, left to right.
5. At the right edge of the pad, go back to the left and keep writing **on the same line**.
6. Where the finger is vertically on the pad does not matter for the line. The writing stays on the current line.
7. Enter starts a new line.
8. Turn Caps Lock off. The trackpad is a normal mouse again, to edit the board, for example with different colours.

The spike built steps 1 to 7. Step 8 (editing) is not built.

## Owner preferences

- **As few key presses as possible.** The owner rejected pressing Space after every word. Going back to the left must be enough.
- In discussions, ask one question at a time and stay on it until the owner moves on.
- Follow the owner's usual workflow: `planning-docs` skill, `docs/spec.md`, `docs/plan.md`, `docs/tasks.md`, approval at each step.
- The owner wants a **phased approach**. There are many more features to come. Each phase should be small enough to finish and use.

## How the spike works

.NET 10 WPF app, one project: `src/TrackpadWhiteboard.App`. Strict analysis settings in `Directory.Build.props` (warnings are errors). No tests.

| File | Role |
| --- | --- |
| `TouchpadReader.cs` | Registers for Raw Input from precision touchpads (HID usage page 0x0D, usage 0x05). Follows the first finger only. Raises FingerDown, FingerMoved and FingerUp with positions from 0 to 1. |
| `TouchpadDevice.cs` | Parses one device's HID reports with the `HidP_*` functions: X, Y, tip switch, contact ID, contact count. Handles reports that split one frame of fingers ("hybrid mode"). |
| `Interop/` | `LibraryImport` declarations and Win32 structures. The HID structure offsets were checked against the `hidpi.h` documentation on Microsoft Learn. |
| `LineWriter.cs` | All placement logic: the writing window, the caret, when to move along the line, the letter gap, new lines, and the shift for round letters. Pure logic with no WPF controls, so it is the easiest part to unit test. |
| `PointSmoother.cs` | Exponential smoothing of points. WPF `FitToCurve` smooths the drawn stroke as well. |
| `MainWindow.xaml(.cs)` | `InkCanvas` board, Caps Lock handling, cursor lock (`ClipCursor`), tuning keys and status bar. |

## What was tried, and what was learned

In the order it happened:

1. **Drawing in Paint with the touchpad** gave unreadable writing. Causes: pointer acceleration (the mouse moves by speed, not position), holding the button with the other hand, and stray lines between letters. Absolute mapping and pen-up-on-lift fixed all three.
2. **Letter gap of 12%** of the line height crowded letters. Changed to 30%.
3. **A full word space on every jump back to the left** was proposed by the owner and rejected: the owner also jumps back in the middle of a word ("Oll" then "ie"), which would split the word.
4. **Edge mode only** (move along on a jump of more than 40% of the pad width): short words like "I", "At" and "is" stacked on top of each other, because the jump back is small.
5. **Pause mode only, 0.7 s**: worked, but too slow. At normal speed the owner's pause between words is shorter. The owner settled on **0.3 s**.
6. **Edge or pause combined** became the default.
7. **Slow writing spread letters apart** ("L a z y"). Fixed: a pause moves along only when the new stroke starts to the left of the ink, where it would overlap.
8. **Round letters filled the word gap** ("JumpsOver"). The first point of a moved-along stroke was placed at the caret, but "O" curves left from its first point. Fixed: the stroke is shifted on lift so that its left edge is at the caret.

After these steps the owner wrote full sentences over three lines and was happy with the result.

## Known weak points

None of these were reported as a problem by the owner, but the real app should consider them:

- Only the first stroke after moving along is shifted. A later stroke of the same letter that reaches left of the caret (for example a long "T" crossbar) is not corrected.
- A slow crossbar, or an "i" dot added after the word is finished, can be placed after the ink instead of on its letter.
- Wide letters whose later strokes start far to the left (the bowls of "B", "R", "P") could trigger the 40% edge rule. Not seen in testing.
- In some screenshots, a capital "T" looked like a "1". Not investigated: it may be the owner's handwriting.
- The stroke shift happens when the finger lifts, so the letter jumps slightly.
- If the touchpad never sends a "finger up" report, the finger stays tracked and the next touch continues the old stroke. Not seen in testing.
- No scrolling: writing past the edge of the window goes off screen.
- No saving: everything is lost when the app closes.
- Tap-to-click still sends clicks while writing. The app ignores them.
- The HID preparsed data is never freed. This is fine while the app has one board for its whole lifetime.

## Open decisions

1. **Feature list.** The owner has many more features in mind. They were not collected in the spike session. Ask the owner for them first.
2. **Phases.** Group the features into phases. Phase 1 should be a whiteboard the owner can use every day.
3. **Grow or restart.** Decide whether the real app grows from the spike code or starts clean and reuses parts. `TouchpadReader`, `TouchpadDevice` and `LineWriter` are the parts most worth keeping.
4. **Tuning keys.** Decide whether the tuning keys and status bar stay in the real app, become settings, or are removed.
