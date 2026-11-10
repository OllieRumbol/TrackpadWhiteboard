# Trackpad Whiteboard, phase 1 — Tasks

> **What this document is for:** It answers *what do we do next?* It splits the approved design into small, verifiable steps and tracks progress against them.

Last updated: 2026-11-10

Phase 1 of the project is split into four stages here. They are called stages so that the names do not clash with phases 2 and 3 in spec section 3.

Each task is one working session and one pull request into `main`. "The owner's check" means the owner tries the change on the laptop's touchpad, and the result goes in the pull request.

## Stage 1 — Writing

Goal: the owner passes the writing test in spec section 6.2.1. Nothing else starts until this is true (spec section 3, build order).

- [ ] **T1 — Add the test project and the CI workflow**
  - Refs: plan 3.1, plan 7.1, plan 8
  - Depends on: —
  - Done when:
    - `tests/TrackpadWhiteboard.App.Tests` is an MSTest project with the Windows target. It is in `TrackpadWhiteboard.slnx`, and it can see the app's internal types.
    - `Directory.Packages.props` holds the package versions, and `global.json` pins the .NET 10 SDK.
    - One test passes on `LineWriter` as it is today: Enter starts a new line at the same left end.
    - A GitHub Actions workflow runs `dotnet build` and `dotnet test` on `windows-latest` for each pull request and for `main`, with the SDK from `global.json`. It passed on this task's pull request.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`
  - Notes: After the workflow has run once, ask the owner before adding it as a required status check on `main` (plan 8).

- [ ] **T2 — Split the spike's main window**
  - Refs: plan 3.1, plan 3.2, spec 6.1.8 item 1
  - Depends on: T1
  - Done when:
    - The files are in the folders in plan 3.1: `Input/`, `Writing/` and `Interop/`.
    - `BoardScreen` holds the Caps Lock and cursor lock flow. `MainWindow` holds the current screen and the window message hook.
    - `KeyMap` maps Space, Enter, Backspace and Delete. The tuning keys stay outside it.
    - The tuning keys and the status bar still work, from outside `MainWindow`.
    - The owner's check: one line of spec section 6.2.1 writes the same as before.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`, then the owner's check
  - Notes: No change in behaviour.

- [ ] **T3 — Fix the touchpad input defects**
  - Refs: spec 4.1 defect 6, spec 6.1.1 items 1–5, spec 6.1.2 items 1–2, spec 6.6, plan 3.2 (`TouchpadReader.cs`, `MainWindow.xaml.cs:60`), plan 3.3 (`ContactTracker`, `TouchpadDetector`), plan 3.5 (clicks in writing mode, finding the touchpad), plan 4, plan 5, plan 7.2 tests 8, 9 and 21
  - Depends on: T2
  - Done when:
    - The frame logic is in `ContactTracker`, and tests feed it frames.
    - When a complete frame does not hold the tracked finger, the stroke ends, and the next touch starts a new stroke (test 8).
    - A second finger has no effect on a stroke (test 9).
    - Tap-to-click has no effect on the board in writing mode.
    - With no precision touchpad, or when Raw Input registration fails, the board screen shows the message in plan 4 when Caps Lock turns on (test 21).
    - The status bar shows the time from the touchpad report to the next rendered frame.
    - The owner's check: spec section 6.1.1 items 1 to 5 hold, and tap-to-click does nothing.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`, then the owner's check
  - Notes:

- [ ] **T4 — Record touchpad input**
  - Refs: plan 3.1, plan 3.4 (recording), plan 3.5 (finding and labelling), plan 6 (recordings)
  - Depends on: T3
  - Done when:
    - In writing mode, when recording is on, each touchpad event and each Enter and Space press goes to a new file in the local `Recordings\` folder, in the format in plan 3.4.
    - A test shows that a recording reads back as the same events.
    - The app makes a picture of a recording's strokes, numbered in order, for labelling.
    - The status bar time from T3 does not grow while recording.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`
  - Notes: Plan 3.5 does not say how recording is turned on, or the format of the labels. Propose both in this task, and add them to plan 3.4 with the owner's approval.

- [ ] **T5 — Record the owner's writing-test lines**
  - Refs: spec 6.2.1, plan 3.5, plan 6
  - Depends on: T4
  - Done when:
    - The owner has recorded each of the six lines in spec section 6.2.1 at least twice, at normal speed. Enter at a word gap is allowed, as in T11.
    - Each recording has labels, made from its numbered picture.
    - The recordings and labels are in `tests/TrackpadWhiteboard.App.Tests/TestData/Recordings/`.
  - Verify: The owner confirms that each committed recording holds only the test lines.
  - Notes: Manual task. Recordings of real notes never go in the repository (plan 6).

- [ ] **T6 — Move `LineWriter` to the planned interface**
  - Refs: plan 3.3, plan 3.5 (word boundary methods), plan 5, spec 4.1 defect 10, spec 6.1.2 items 13–14, spec 6.1.6 item 6, plan 7.2 test 1
  - Depends on: T2
  - Done when:
    - `LineWriter` has the methods and types in plan 3.3, including `GapKind` and the `WordBoundaryMethod` setting. The screen applies the `StrokePlacement` it returns.
    - The worked examples in spec section 6.2 for Enter, Space, and Backspace after Enter pass as tests.
    - The status bar shows each move-along decision and its reason.
    - The owner's check: writing is the same as before, except the Backspace fix.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`, then the owner's check
  - Notes:

- [ ] **T7 — Replay the recordings and score them**
  - Refs: plan 3.5, plan 7.1, plan 7.2 test 3, spec 6.2.1 (errors)
  - Depends on: T5, T6
  - Done when:
    - The rules that turn each error kind in spec section 6.2.1 into a measure are in plan 3.5, with the owner's approval. For example, the gap width that counts as a split word or as joined words.
    - A test replays each recording through `LineWriter` and compares the result with its labels.
    - For each recording and each word boundary method that exists, it reports the errors in spec section 6.2.1: split words, joined words, overlaps and misplaced strokes.
    - The score of the current behaviour is in the pull request, as the baseline.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`
  - Notes:

- [ ] **T8 — Keep later strokes on their letter**
  - Refs: spec 4.1 defects 2–5, spec 6.1.2 items 8–12, spec 6.2 ("Lazy", "O", "T", "in", "B"), plan 3.5 (dots and crossbars), plan 7.2 tests 5 and 7
  - Depends on: T7
  - Done when:
    - The five worked examples in Refs pass as tests.
    - A later stroke that reaches left of the caret is corrected, not only the first stroke after moving along.
    - The replay has no more misplaced strokes than the baseline.
    - The replay reports how many strokes jump at lift, as plan 3.5 says.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`
  - Notes: If the first test in plan 3.5 fails on the recordings, propose a replacement and update plan 3.5. A rare jump at lift is accepted. The owner judges spec section 6.1.2 item 12 in T11.

- [ ] **T9 — Tell a jump in a word from a new word: method 1**
  - Refs: spec 4.1 defect 1, spec 6.1.2 items 3–7, spec 6.1.2.1 (method 1), spec 6.2 ("I am", "Ollie", "Pythagoras", "Hello world"), plan 3.5 (moving along and the gap), plan 7.2 tests 2 and 6
  - Depends on: T8
  - Done when:
    - The four worked examples in Refs pass as tests.
    - Two short words do not overlap (test 6).
    - The replay score for method 1 is in the pull request, next to the baseline.
    - On the replay, each run of the six lines has no more than 2 errors.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`
  - Notes: If the replay has more than 2 errors in a run, stop and report. The next method is under "Blocked".

- [ ] **T10 — Show the touchpad outline and the caret**
  - Refs: spec 6.1.1 item 6, plan 3.6 (signature, colour, sizes, motion), mock-ups `board-writing-mode.dc.html` and `touchpad-outline-steps.dc.html`
  - Depends on: T6
  - Done when:
    - In writing mode, a faint rounded outline shows the writing window, and the caret shows where the next stroke goes.
    - The outline stays still while a pass is written. It jumps, with no slide, when the writing window moves along.
    - It is drawn on a layer above the ink that is not saved, and the ink under it stays easy to read.
    - It does not show in mouse mode.
    - The owner signs off the look.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`, then the owner's check
  - Notes:

- [ ] **T11 — Run the writing test**
  - Refs: spec 4.1 defect 13, spec 6.2.1, spec 6.1.1 item 6, spec 6.1.2 item 12, spec 6.5 item 1, plan 7.2 owner check 1, plan 9 (outline and gesture risks)
  - Depends on: T9, T10
  - Done when:
    - The owner writes the six lines on two runs in a row, with no more than 2 errors in each run.
    - The owner says that the writing feels free and easy.
    - The owner says that the outline helps and does not distract, and that letters do not jump when the finger lifts.
    - The status bar times are within 50 ms, and the owner sees no lag.
    - The owner says whether a capital "T" still looks like a "1" (spec 4.1 defect 13), and whether a three-finger gesture fired by mistake.
    - The errors, the times and the tuning values used are in the pull request.
  - Verify: The owner's check, as above
  - Notes: Manual task. The right edge comes in T15, so the owner may press Enter at a word gap near the right side of the screen (spec section 6.2.1). If the test fails, stop and report. The next method is under "Blocked". The owner can change the numbers and lines in spec section 6.2.1.

- [ ] **T12 — Fix the writing values and remove the tuning keys**
  - Refs: spec 4.1 defect 11, spec 6.1.2 item 15, spec 6.1.8 items 1, 2 and 4, plan 2, plan 3.2, plan 3.3 (`KeyMap`), plan 3.5 (word boundary methods, moving along and the gap), plan 7.2 test 22
  - Depends on: T11
  - Done when:
    - The values from the passed writing test are constants in code. The default look is still the spike's thick black pen with smooth curves.
    - The tuning keys, the status bar, and the `AdvanceMode` and `WordBoundaryMethod` values that did not win are removed.
    - Letter and punctuation keys do not change how writing behaves (test 22, for the keys that exist so far).
  - Verify: `dotnet test TrackpadWhiteboard.slnx`
  - Notes: The keys `1`–`4`, `E`, `Y` and `0` are free for stage 2 only after this task.

## Stage 2 — Board

Goal: the owner has a large board, can zoom and move around it, and writing stays on the screen (spec section 3, goal 2).

- [ ] **T13 — Make a large board with zoom and view moves**
  - Refs: spec 6.1.3 items 1–5 and 9, plan 3.1 (`Boards/`), plan 3.3 (keys), plan 3.4 (sizes), plan 3.5 (zoom and view, pinch and scroll), plan 9 (pinch risk)
  - Depends on: T12
  - Done when:
    - A new board is 4 times the screen's width and height, and opens at normal zoom at its top left corner.
    - In mouse mode, pinch and `+`/`-` zoom, two-finger scroll and the arrow keys move the view, and `0` shows the whole board.
    - Zoom and view moves do not change the ink, and ink stays sharp at every zoom level.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`, then the owner's check of pinch and scroll
  - Notes: Check pinch first (plan 9): it must arrive as wheel messages with `MK_CONTROL` set (plan 3.5). If it does not, stop and report. The plan does not set the lowest and highest zoom. Propose them in this task, and add them to plan 3.5.

- [ ] **T14 — Write at any zoom, with the writing on the screen**
  - Refs: spec 6.1.3 items 6–8, spec 6.6 (pointer outside the board, window changes size), plan 3.5 (same size on the screen), plan 7.2 test 13
  - Depends on: T13
  - Done when:
    - New writing has the same line height and pen thickness on the screen at every zoom (test 13).
    - In writing mode, the view moves so that the current stroke, the caret and the outline stay on the screen. The owner presses no key for this.
    - The zoom never changes by itself in writing mode.
    - When the pointer is outside the board, a new line starts at the nearest point on the board.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`, then the owner's check
  - Notes:

- [ ] **T15 — Continue at a right edge and grow the board**
  - Refs: spec 6.1.4 items 1–4, spec 6.2 ("triangle", word wider than a column), plan 3.3 (`WordMove`), plan 3.5 (grown board, moves during saving), plan 7.2 tests 10–12, plan 9 (word moves)
  - Depends on: T14
  - Done when:
    - At a right edge, writing continues on a new line below, at the same left end, with no key press.
    - The word in progress moves to the new line after the finger lifts (test 10).
    - A word wider than the space splits at the edge (test 11).
    - A new line below the bottom makes the board one screen taller (test 12).
    - A move replaces strokes with moved copies.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`, then the owner's check
  - Notes:

- [ ] **T16 — Add the four pen colours**
  - Refs: spec 6.1.6 items 1, 2 (keys), 4 (new board) and 5, plan 3.3 (keys), plan 3.6 (ink colours)
  - Depends on: T12
  - Done when:
    - Keys `1` to `4` select black, blue, red and green with one press, in writing mode and in mouse mode.
    - New strokes use the selected colour. Existing strokes keep theirs.
    - A new board starts with black.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`
  - Notes: The palette and the shown colour (spec 6.1.6 items 2–3) come with the tray in T22.

- [ ] **T17 — Add undo, redo, the eraser and clear**
  - Refs: spec 4.1 defects 9 and 10, spec 6.1.6 items 6–9, spec 6.6 (Backspace rows, and leaving a board), plan 3.1 (`UndoHistory`), plan 3.3 (keys and `LineWriter`), plan 3.5 (clear the board, lines after undo and redo, clicks in writing mode), plan 7.2 tests 15 and 24
  - Depends on: T15, T16
  - Done when:
    - Backspace undoes the last stroke and moves the caret back on that stroke's line, in both modes.
    - `Y` brings back the last stroke that was undone or erased, and the caret goes to the end of that stroke's line.
    - After a clear is undone, writing starts on a new line below the ink.
    - `E` turns the eraser on or off in mouse mode, and a click erases one whole stroke.
    - With the eraser on, a tap-to-click in writing mode erases nothing (test 24).
    - Delete clears the board, and one undo brings it back.
    - Backspace with no strokes, or during a stroke, does nothing.
    - Test 15 passes.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`
  - Notes:

## Stage 3 — Keep

Goal: every board is kept, and the owner finds and opens it from the home screen (spec section 3, goal 3).

- [ ] **T18 — Save and open boards**
  - Refs: spec 6.1.6 item 4 (opened board), spec 6.1.7 items 1, 4 and 5, plan 2 (data folder), plan 3.3 (`BoardStore`, `BoardSnapshot`, stroke IDs), plan 3.4, plan 3.5 (ink store, details store), plan 4 (saving and damaged files), plan 7.2 tests 16–17
  - Depends on: T17
  - Done when:
    - `BoardStore` saves and opens boards in the folders and files in plan 3.4. It takes the data folder in its constructor, and tests give it a temporary folder.
    - Strokes keep their IDs as custom stroke data, and a moved copy keeps its ID.
    - Save then open gives the same strokes, colours, layout, name, view and pen colour (test 16).
    - Files are written to a `.tmp` name first, and a failed save leaves the old files readable (test 17).
  - Verify: `dotnet test TrackpadWhiteboard.slnx`
  - Notes:

- [ ] **T19 — Save while the owner writes**
  - Refs: spec 6.1.7 items 2–3, spec 6.5 items 3–4, spec 6.6 (board cannot be saved, laptop sleeps), plan 3.3 (`BoardSaver`), plan 3.5 (saving, saving steps, moves during saving), plan 4, plan 7.2 test 20
  - Depends on: T18
  - Done when:
    - `BoardSaver` follows the saving steps in plan 3.5: every change in step 1 is saved from a snapshot on a background thread, at most every 3 seconds, one save at a time, without the stroke being drawn (test 20, with `FakeTimeProvider`).
    - The board saves at once when the window stops being active, when the owner leaves the board, and when the app closes.
    - A failed save shows a message, keeps the board open, tries again and is logged. If the last save fails when the owner leaves or closes, the app stays on the board.
    - When the app is stopped by force, no more than the last 5 seconds of writing are lost.
    - The owner's check: no delay while writing on a large board.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`, then the owner's check
  - Notes:

- [ ] **T20 — Build the home screen**
  - Refs: spec 6.3, spec 6.4.1, spec 6.4.2 (back to home), spec 6.5 item 5 (opening sign), spec 6.6 (Caps Lock on at start, damaged file, same name, same board twice), plan 3.1 (`Screens/`), plan 3.3 (Esc), plan 3.4 (`preview.png`), plan 3.5 (preview picture), plan 3.6 (empty home screen, access), plan 4 (names, rename or delete fails), plan 7.2 tests 18–19, mock-up `home.dc.html`
  - Depends on: T19
  - Done when:
    - The app starts on the home screen. It lists every board, most recently changed first, with a preview, name and last-changed time (test 19).
    - The preview is made when the owner leaves a board or closes the app.
    - New board opens a blank board at once, named from the date and time.
    - Open, rename and delete work. Delete asks first. Names, and a failed rename or delete, behave as plan 4 says.
    - The empty state and damaged boards show as spec section 6.4.1 says (test 18).
    - While a board opens, the app shows that it is opening.
    - Esc in mouse mode goes back to the home screen.
    - Each button has an automation name and a left-to-right focus order.
    - The owner signs off the look against the mock-up, and the empty state, which has no mock-up.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`, then the owner's check
  - Notes: If the task grows beyond one session, split the previews into their own task.

- [ ] **T21 — Make the app safe to close, sleep and restart**
  - Refs: spec 6.5 item 8, spec 6.6 (second copy, app closes, laptop sleeps, window size, other screen), plan 3.5 (one running copy, start-up), plan 4, plan 5, plan 6, plan 7.2 test 25
  - Depends on: T20
  - Done when:
    - A second copy brings the first one to the front and stops.
    - One handler logs an unexpected exception, releases the cursor, tries a last save and shows a message.
    - Closing in writing mode releases the cursor and saves.
    - `Logs\app.log` holds errors, warnings, the slowest writing response in each session and each save over 1 second. It holds no ink or board names, and it is kept under the size limit in plan 5.
    - The app's assembly references no networking types (test 25).
    - The owner's check: sleep and lock in writing mode end the stroke, free the cursor and save. A window size change or a move to another screen does not change the ink.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`, then the owner's check
  - Notes: If sleep or lock does not run the deactivate flow, handle the Windows session events (plan 4).

- [ ] **T22 — Add the tool tray and the writing sign**
  - Refs: spec 6.1.6 items 2–3, spec 6.4.2, spec 6.5 items 6–7, plan 3.6 (tools, type, colour, sizes, access), mock-ups `board-mouse-mode.dc.html` and `board-writing-mode.dc.html`
  - Depends on: T20
  - Done when:
    - In mouse mode, a floating tray at the bottom centre holds each tool in plan 3.6, with its key. One click does each action, except Write, which is a label only.
    - The tray shows the selected pen colour.
    - In writing mode the tray hides, and the "Writing" sign with the pen colour shows.
    - Nothing sits in the top-left corner.
    - Each tray button has an automation name and a left-to-right focus order.
    - The owner signs off the look against the mock-ups.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`, then the owner's check
  - Notes:

## Stage 4 — Layouts and daily use

Goal: the three-column layout and the settings view work, the numbers in spec section 6.5 are met, and the owner uses the app for 5 working days (spec section 3, goals 4 and 5).

- [ ] **T23 — Add the three-column layout**
  - Refs: spec 6.1.4 item 5, spec 6.1.5, spec 6.4.1 (new board), plan 3.4 (`titleBoxHeight`), plan 7.2 test 14, mock-ups `three-columns.dc.html` and `home.dc.html`
  - Depends on: T20
  - Done when:
    - New board lets the owner choose a blank board or the three-column layout.
    - The three columns have equal width, with title boxes 3 lines high (test 14).
    - The dividing lines are background. Pens, the eraser and undo do not change them.
    - A new line starts in the section under the pointer. A right edge is the section's right edge.
    - A line below a title box continues into the list below.
    - Columns grow with the board, and title boxes do not.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`, then the owner's check
  - Notes:

- [ ] **T24 — Add the settings view**
  - Refs: spec 6.1.8 item 3, plan 2, plan 3.1 (`Settings/`, `SettingsView`), plan 4 (`settings.json`), plan 7.2 test 23
  - Depends on: T20
  - Done when:
    - A simple settings view changes pen thickness and line height, and they are kept in `settings.json`.
    - A `settings.json` that cannot be read gives the defaults, and is logged (test 23).
    - The owner signs off the look.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`, then the owner's check
  - Notes: No mock-up exists, and the plan does not say where the view opens from. Show the owner a mock-up first, and record the result in plan 3.6.

- [ ] **T25 — Check the numbers in spec section 6.5**
  - Refs: spec 6.5 items 1–5, plan 5, plan 9
  - Depends on: T19, T21
  - Done when:
    - A test helper makes a board with 20,000 strokes of 60 points each, as the probe in plan 9 did.
    - On that board, writing response, saving and opening are measured on the owner's laptop.
    - Each number in spec section 6.5 is met, or the owner approves a change to it.
    - The results are in the pull request.
  - Verify: The measurements, as above
  - Notes: If opening is slow, the next option is in plan 9.

- [ ] **T26 — Install the app and run the writing test again**
  - Refs: plan 7.2 owner checks 1–2, plan 8 (install), spec 6.2.1
  - Depends on: T22, T23, T24, T25
  - Done when:
    - `README.md` says how to publish the app to a dated local folder, add a desktop shortcut, and roll back (plan 8).
    - The owner passes the writing test again, with zoom and saving in place, and with no Enter inside a line.
    - The owner checks pinch, two-finger scroll and every key in plan 3.3.
  - Verify: The owner's check, as above
  - Notes: Manual task.

- [ ] **T27 — Use the app for five working days**
  - Refs: spec 3 goal 5, spec 9, plan 7.2 owner check 3, plan 9
  - Depends on: T26
  - Done when:
    - The owner has used the app for their own notes for 5 working days, and has lost no work.
    - Each item in spec section 9 is true.
    - The owner's findings, including on word moves at a right edge, are in the pull request.
  - Verify: The owner's check, as above
  - Notes: Manual task. When this task is ticked, run `/retrospective`.

---

## Blocked

These tasks are needed only if method 1 does not pass. They wait for T9 or T11. When one is needed, move it above T11, after the method that failed. When the writing test passes, remove the rest.

- [ ] **T28 — Tell a jump in a word from a new word: the row rule**
  - Refs: spec 6.1.1 item 6 (row outline), spec 6.1.2 item 1, spec 6.1.2.1 (method 2 and the row rule), plan 3.5 (row rule mapping), plan 7.2 test 4, plan 9 (row rule risk)
  - Depends on: T9 or T11 fails with method 1
  - Done when:
    - A lower row continues the word, and the same or a higher row starts a new word (test 4).
    - The outline shows the part of the board for each touchpad row.
    - The replay score is in the pull request.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`, then the owner's check
  - Notes: Try it in a short session before fully building it (plan 9).

- [ ] **T29 — Tell a jump in a word from a new word: a longer pause**
  - Refs: spec 6.1.2.1 (method 3), plan 7.2 test 2
  - Depends on: T28 fails
  - Done when:
    - A longer pause starts a new word, and a short jump back continues the word.
    - The replay score is in the pull request.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`
  - Notes:

- [ ] **T30 — Tell a jump in a word from a new word: a touchpad signal**
  - Refs: spec 6.1.2.1 (method 4), plan 3.5 (touchpad signal), plan 7.2 test 2
  - Depends on: T29 fails
  - Done when:
    - A tap with a second finger starts a new word.
    - The replay score is in the pull request.
  - Verify: `dotnet test TrackpadWhiteboard.slnx`, then the owner's check
  - Notes: If this method also fails, stop. Spec section 8 item 6 says that the spec then changes.
