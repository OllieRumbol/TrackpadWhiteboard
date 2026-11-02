# Trackpad Whiteboard

A Windows whiteboard where you write with one finger on the laptop touchpad.

Point the mouse where you want to write and turn Caps Lock on. The cursor locks in place, and the touchpad becomes a writing window on the current line. Lift your finger and go back to the left to keep writing along the line. Turn Caps Lock off to use the mouse again.

## Status

This is a **spike**: a quick experiment to find out whether writing on the touchpad feels good enough to build a full app. It has no tests, no saving and no editing. The real app comes next, from `docs/spec.md`, `docs/plan.md` and `docs/tasks.md`.

It needs a precision touchpad. To check, open *Settings > Bluetooth & devices > Touchpad*.

## Run

```bash
dotnet run --project src/TrackpadWhiteboard.App
```

## Keys

| Key | Action |
| --- | --- |
| Caps Lock | Writing mode on or off |
| Enter | New line |
| Space | Word gap (optional) |
| Backspace | Undo the last stroke |
| Delete | Clear the board |
| `[` `]` | Pen thickness |
| `-` `+` | Line height |
| `,` `.` | Letter gap |
| `;` `'` | Pause time |
| `S` | Smoothing level |
| `A` | Move-along mode: Edge, Pause or EdgeOrPause |

## What the spike found

Tested by the owner, writing full sentences on their own laptop (2 November 2026).

| Topic | Finding |
| --- | --- |
| Input | Read raw finger positions from the touchpad through Raw Input, and map them absolutely to the writing window. Pen down when the finger touches, pen up when it lifts. This fixed the problems of drawing with a mouse in Paint: pointer acceleration, a held button and stray connecting lines. |
| Look | A thick black pen (6 px), Medium smoothing and curve fitting give readable handwriting. |
| Line layout | The full touchpad maps to one line. Letter gap: 30% of the line height. |
| Moving along | EdgeOrPause works best. A jump back to the left of more than 40% of the pad width moves along at once. A pause of 0.3 s or more moves along, but only when the new stroke starts to the left of the ink. Without that last condition, slow writing spread letters apart. |
| Round letters | Letters like "O" start at the top and curve left. A stroke that moves along is shifted so that its left edge, not its first point, is at the caret. Without this, the "O" filled the word gap. |
| Keys | The owner wants as few key presses as possible. Space is optional: going back to the left is enough. |
| Still open | Colours, editing, saving, scrolling a long page, and whether the tuning keys stay in the real app. |
