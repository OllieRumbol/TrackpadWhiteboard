# Trackpad Whiteboard, phase 1 — Specification

> **What this document is for:** It answers *what are we building, and why?* It is agreed before any design or code, and it is the source of truth for requirements.

Status: Approved
Last updated: 2026-11-06

---

## 1. Purpose

Trackpad Whiteboard is a Windows app for one person: the owner, who cannot grip a pen. The owner writes on a large board with one finger on the laptop touchpad. Phase 1 turns the spike into a whiteboard that the owner can use every day. The writing experience is the core of the app, and it comes before every other feature.

---

## 2. Background

### 2.1 Why the app exists

At school, the one place where writing was easy for the owner was a whiteboard on a wall, with a big fat pen. The owner has not written by hand for many years and misses the freedom of it: putting notes down quickly, using lots of space. Microsoft Whiteboard is no longer an option for the owner.

Every requirement serves one goal: **writing must feel free and easy**. Any extra key press, wait or correction works against that goal.

### 2.2 The way of writing

1. Point the mouse where you want to start writing.
2. Turn Caps Lock on. The cursor freezes where it is.
3. Write with one finger on the touchpad, left to right.
4. At the right edge of the touchpad, go back to the left and keep writing on the same line.
5. Where the finger is vertically on the touchpad does not change the line.
6. Enter starts a new line.
7. Turn Caps Lock off. The touchpad is a normal mouse again, to edit the board.

### 2.3 Terms

| Term | Meaning |
| --- | --- |
| Stroke | The ink from one finger touch, from finger down to finger up. |
| Line | A row of writing. It has a left end, a top and a height. |
| Writing window | The area of the board that the touchpad maps to. It is on the current line. |
| Caret | The place on the line where the next stroke goes when writing moves along. |
| Moving along | Moving the writing window to the right of the ink, so that the writer can continue. |
| Pass | The ink written in one position of the writing window. A long word takes several passes. |
| Letter gap | The gap between letters in a word. |
| Word gap | The gap between words. It must look clearly larger than a letter gap. |
| Section | One area of a layout, such as a title box or a list. |
| View | The part of the board that is on the screen, at the current zoom. |

### 2.4 What the spike learned

The owner tested the spike on their own laptop on 2 November 2026, and wrote full sentences over three lines. The word lengths in section 6.2 were not tested in a planned way.

| Topic | Finding |
| --- | --- |
| Input | Drawing in Paint with the touchpad gave unreadable writing, because of pointer acceleration, a held button and stray lines between letters. Absolute mapping of raw finger positions, with pen up on lift, fixed all three. |
| Look | A thick black pen (6 px), Medium smoothing and curve fitting give readable handwriting. |
| Line | The full touchpad maps to one line. |
| Letter gap | 12% of the line height crowded the letters. 30% works. |
| Word gaps | The owner rejected pressing Space after every word. Going back to the left must be enough. |
| Full word gap on every jump | Rejected. The owner also jumps back in the middle of a word ("Oll" then "ie"), and this split the word. |
| Jump only | Moving along only on a jump of more than 40% of the pad width made short words ("I", "At", "is") stack, because the jump back is small. |
| Pause only | 0.7 s worked but was too slow. The owner settled on 0.3 s. |
| Slow writing | A pause spread letters apart ("L a z y"). A pause now moves along only when the new stroke starts to the left of the ink. |
| Round letters | "O" starts at the top and curves left, so it filled the word gap. A moved-along stroke is now shifted so that its left edge is at the caret. |

---

## 3. Goals and non-goals

### Goals

1. **Writing:** the owner passes the writing test in section 6.2.1, with words from 1 to 12 letters, using no key press between words.
2. **Room:** the owner never runs out of room on a board.
3. **Keep:** every board is kept when the app closes, and the owner can find and open it from the home screen.
4. **Organise:** the owner can use the three-column layout in section 6.1.5.
5. **Daily use:** the owner uses the app for their own notes for 5 working days and does not lose work.

**Priority rule:** when writing conflicts with any other feature, writing wins.

**Build order:** the writing requirements (sections 6.1.1 and 6.1.2) are built and pass the writing test before work on the other features starts.

### Non-goals

Later phases. Each phase is small enough to finish and use:

* **Phase 2, more layouts:** portrait boards, and layouts with halves, thirds, quarters and sixths.
* **Phase 3, shapes and diagrams:** triangles and other shapes, for example for a Pythagoras problem.

Later, in no phase yet:

* Changing the colour of existing strokes.
* Selecting and moving ink.
* Rubbing out part of a stroke by dragging.
* Duplicating a board, folders or tags, and searching boards by name.

Not planned:

* Handwriting recognition or conversion to typed text.
* Typing text on the board.
* Export, printing or sharing.
* Cloud storage, sync or more than one user.
* A pen, a touch screen or a mouse as a writing device. A mouse is for pointing only.
* Mac, Linux or tablets.

---

## 4. Current state of the code

| Project / Area | Purpose | State |
| --- | --- | --- |
| `src/TrackpadWhiteboard.App/TouchpadReader.cs` | Registers for touchpad input and follows the first finger. | Works in the spike. No tests. |
| `src/TrackpadWhiteboard.App/TouchpadDevice.cs` | Parses one device's touchpad reports. | Works in the spike. No tests. |
| `src/TrackpadWhiteboard.App/Interop/` | Windows API declarations. | Works in the spike. |
| `src/TrackpadWhiteboard.App/LineWriter.cs` | Places strokes on a line. Holds all the rules for moving along. | Works for short tests. Has the writing defects below. No tests. |
| `src/TrackpadWhiteboard.App/PointSmoother.cs` | Smooths finger movement. | Works in the spike. No tests. |
| `src/TrackpadWhiteboard.App/MainWindow.xaml(.cs)` | Board, Caps Lock handling, cursor lock, tuning keys and status bar. | Spike quality. |
| Tests | None. | Missing. |

### 4.1 Defects and gaps

Writing:

1. **One gap for every move along.**
   `LineWriter.cs:60` — moving along always leaves one letter gap after the ink. A jump in the middle of a word and a jump between words get the same gap. Only the Space key adds a larger gap.
2. **Later dots and crossbars can move to the wrong place.**
   `LineWriter.cs:156-158` — a stroke that starts to the left of the ink after a pause of more than 0.3 s moves along. An "i" dot or a slow crossbar then lands after the ink, not on its letter.
3. **Wide letters can trigger a jump.**
   `LineWriter.cs:153` — a stroke that starts more than 40% of the pad width to the left of the previous stroke's right edge moves along. The bowls of "B", "R" and "P" could do this. Not seen in testing.
4. **Only the first stroke after moving along is shifted.**
   `LineWriter.cs:130` — a later stroke of the same letter that reaches left of the caret, such as a long "T" crossbar, is not corrected.
5. **Letters jump when the finger lifts.**
   `MainWindow.xaml.cs:247-250` — the shift for round letters happens on lift, so the letter moves slightly.
6. **A lost finger-up breaks the next touch.**
   `TouchpadReader.cs:138-162` — if the touchpad never reports the finger up, a next touch with the same contact ID continues the old stroke, and a touch with another contact ID is ignored. Not seen in testing.

Board:

7. **No scrolling or zoom.**
   `MainWindow.xaml.cs:160-161` — the start of a line stays inside the window, and writing past the edge goes off screen.
8. **No saving.** Everything is lost when the app closes.
9. **Delete clears the whole board at once.**
   `MainWindow.xaml.cs:95-96` — there is no confirmation and no undo.
10. **Undo does not move back a line.**
    `MainWindow.xaml.cs:268-271` — after Enter, undoing a stroke on an earlier line does not move the caret back.
11. **Tuning keys are always active.**
    `MainWindow.xaml.cs:98-127` — letter and punctuation keys change settings with no warning.
12. **One colour only.**
    `MainWindow.xaml.cs:294` — the pen is always black.

Not investigated:

13. **Capital "T".** In some screenshots, a capital "T" looked like a "1". This may be the owner's handwriting.

`TouchpadReader.cs`, `TouchpadDevice.cs` and `LineWriter.cs` are the parts of the spike most worth keeping. Whether the app grows from the spike or starts clean and reuses them is a design decision.

---

## 5. External interfaces

### 5.1 Precision touchpad

The app reads raw touchpad reports from Windows Raw Input: HID usage page `0x0D`, usage `0x05` (`TouchpadReader.cs:11-12`).

Each report gives X, Y, tip switch, contact ID and contact count.

Notes:

* A touchpad can split one frame of fingers over several reports. The first report gives the finger count, and the next reports give a count of zero.
* Tap-to-click still sends clicks while the owner writes.
* A laptop without a precision touchpad cannot use the app.

### 5.2 Caps Lock

The Caps Lock state belongs to Windows and affects other apps. The app reads the state. It does not change it.

---

## 6. Functional specification

### 6.1 Behaviour

#### 6.1.1 Writing mode

1. WHEN Caps Lock is on and a board is open and active, THE SYSTEM SHALL be in writing mode: the cursor is locked and hidden, and the touchpad writes.
2. WHEN writing mode starts and the mouse has moved since writing mode last stopped, THE SYSTEM SHALL start a new line at the pointer. The pointer is at the vertical middle of the line.
3. WHEN writing mode starts and the mouse has not moved, THE SYSTEM SHALL continue the same line.
4. WHEN Caps Lock is turned off, THE SYSTEM SHALL unlock the cursor, show it at the caret and make the touchpad a normal mouse.
5. WHEN the window stops being active, THE SYSTEM SHALL finish the current stroke and unlock the cursor. WHEN the window is active again, writing mode follows the Caps Lock state.
6. WHILE in writing mode, THE SYSTEM SHALL show where the next stroke goes.

#### 6.1.2 Writing

These are the core requirements. See the priority rule in section 3.

1. THE SYSTEM SHALL map the whole touchpad absolutely to the writing window. Pen down is finger down. Pen up is finger up. If the row rule (section 6.1.2.1) is the method used, each row of the touchpad maps absolutely to the writing window instead.
2. THE SYSTEM SHALL follow one finger only: the first one that touches. Other fingers and tap-to-click SHALL have no effect while in writing mode.
3. THE SYSTEM SHALL need no key press to write along a line. Space stays optional.
4. THE SYSTEM SHALL handle words of any length, including words longer than one pass of the touchpad.
5. WHEN the owner goes back to the left in the middle of a word, THE SYSTEM SHALL join the parts with a letter gap, not a word gap.
6. WHEN the owner starts a new word, THE SYSTEM SHALL leave a word gap.
   The app tells these two cases apart with the first method in section 6.1.2.1 that passes the writing test.
7. Short words of 1 or 2 letters SHALL NOT overlap each other.
8. Slow writing SHALL NOT spread the letters of a word apart.
9. A letter that curves left from its first point, such as "O", SHALL NOT fill the gap before it.
10. Later strokes of a letter SHALL land on that letter. This includes crossbars, dots and bowls (`t`, `T`, `i`, `j`, `B`, `R`, `P`), at slow and at normal speed.
11. A dot or crossbar added after a word is finished SHALL land on its letter when that letter is inside the writing window.
12. A letter SHOULD NOT visibly jump when the finger lifts. The owner judges this during the writing test.
13. WHEN the owner presses Space while in writing mode, THE SYSTEM SHALL add a word gap.
14. WHEN the owner presses Enter while in writing mode, THE SYSTEM SHALL start a new line below the current one, at the same left end.
15. The default look SHALL be the spike's: a thick black pen with smooth curves (section 2.4).

##### 6.1.2.1 Telling a jump in a word from a new word

The owner wants no key press between words, and also jumps back in the middle of words. There is no evidence yet that the app can tell these apart. So the owner's writing of the lines in section 6.2.1 is recorded first. Then these methods are tried in this order, and the first one that passes the writing test is used:

1. **The app works it out** from timing and position. The owner does nothing extra.
2. **The row rule.** The owner's height on the touchpad is the signal. See below.
3. **A longer pause means a new word.** A short jump back continues the word.
4. **A signal on the touchpad**, such as a tap, means a new word. It is not a key press.

One medium gap for both cases, as in the spike, is not acceptable. Long words look split, and word gaps are small.

**The row rule**

The touchpad has three rows of equal height: top, middle and bottom. Each row maps to the height of the line, so a row holds about three times as many letters as the whole touchpad does without rows. The rule applies when the owner goes back to the left:

| The finger lands | Result |
| --- | --- |
| In a lower row | Same word, with a letter gap |
| In the same row or a higher row | New word, with a word gap |

| Writing | Finger path | Result |
| --- | --- | --- |
| "I am at" | Middle row, back to the left in the middle row, and again | Three words |
| "Pythagoras" | "Pythag" in the top row, then "oras" in the middle row | One word |
| "at Pythagoras" | "at" in the middle row, "Pythag" in the top row, "oras" in the middle row | Two words |

Known risks, which the writing test checks:

* Letters use one third of the touchpad height, so finger wobble is three times larger in the ink.
* The owner cannot feel the row edges, so letters can move up or down on the line.
* The rule does not tell a short word from the next stroke of a letter when both start in the same row and place. Items 7 and 10 in section 6.1.2 still apply.
* A word that does not fit in the rows below where it starts looks split. This counts as an error in the writing test.
* The owner must judge how long a word is before starting it.

#### 6.1.3 Board and view

1. A new board SHALL be 4 times the width and 4 times the height of the screen at normal zoom. The width does not change after that. The height can grow (section 6.1.4).
2. WHILE in mouse mode, THE SYSTEM SHALL let the owner zoom with a two-finger pinch or the `+` and `-` keys, and move the view with a two-finger scroll or the arrow keys.
3. THE SYSTEM SHALL let the owner see the whole board at once.
4. Zooming and moving the view SHALL NOT change the ink.
5. Ink, dividing lines and the caret SHALL stay sharp at every zoom level that the app allows.
6. WHILE in writing mode, THE SYSTEM SHALL move the view when needed, so that the current stroke and the caret stay on the screen. The owner presses no key for this.
7. New writing SHALL have the same size on the screen at every zoom level, with the same line height and pen thickness on the screen. So writing at a higher zoom is smaller on the board, and more fits on the board. Writing already on the board keeps its size on the board.
8. WHILE in writing mode, THE SYSTEM SHALL NOT change the zoom by itself.
9. A new board SHALL open at normal zoom, at its top left corner.

#### 6.1.4 Writing at the edge of a section or the board

A right edge is the right edge of the section that holds the line. On a blank board, it is the right edge of the board.

1. WHEN a stroke reaches a right edge, THE SYSTEM SHALL start a new line below the current one, at the same left end, with no key press.
2. THE SYSTEM SHALL move the word in progress to the new line, so that words stay whole. The word moves only after the finger lifts.
3. WHEN a word is wider than the space from the line's left end to the right edge, THE SYSTEM SHALL split it at the edge and continue it on the new line.
4. WHEN a new line would be below the bottom of the board, THE SYSTEM SHALL make the board taller, with no key press. Column lines grow with the board. The title boxes do not change. The board does not get shorter again.
5. WHEN a new line in a title box would be below the title box, the line SHALL continue over the dividing line, into the list section below.

#### 6.1.5 Layouts

1. WHEN the owner creates a board, THE SYSTEM SHALL let them choose a blank board or the three-column layout.
2. Every board in phase 1 SHALL start landscape. A board can become taller than it is wide (section 6.1.4).
3. The three-column layout SHALL have three columns of equal width. Each column SHALL have a short title box at the top and a long list section below it. The owner confirmed this drawing:

   ```text
   +---------+---------+---------+
   | title   | title   | title   |
   +---------+---------+---------+
   | long    | long    | long    |
   | list    | list    | list    |
   |         |         |         |
   +---------+---------+---------+
   ```

4. Each title box SHALL be 3 lines of writing high, at normal zoom. This height is fixed when the board is created.
5. The dividing lines SHALL be part of the board's background. Pens, the eraser and undo SHALL NOT change them.
6. A new line starts in the section under the pointer.
7. In phase 1, the layout SHALL NOT change after the board is created.

#### 6.1.6 Pens and editing

1. THE SYSTEM SHALL offer four pen colours: black, blue, red and green.
2. Keys `1` to `4` SHALL select a colour with one press, in writing mode and in mouse mode. In mouse mode, a small palette SHALL also select a colour with one click.
3. THE SYSTEM SHALL show the selected colour on the board screen.
4. A new board SHALL start with black. An opened board SHALL start with the last colour used on it.
5. New strokes SHALL use the selected colour. Existing strokes SHALL keep their colour.
6. WHEN the owner presses Backspace, THE SYSTEM SHALL undo the last stroke and move the caret back to the end of the remaining ink on that stroke's line.
7. WHEN the owner clears a whole board, THE SYSTEM SHALL let them get it back with one undo, or ask first.
8. In mouse mode, one key press SHALL turn the eraser on or off. WHILE the eraser is on, a click on a stroke SHALL erase that whole stroke.
9. WHEN the owner presses the redo key, THE SYSTEM SHALL bring back the last stroke that was undone or erased. One key press does this.

#### 6.1.7 Saving

1. THE SYSTEM SHALL keep each board's strokes, stroke colours, layout, name, last-changed time, last view and last pen colour.
2. THE SYSTEM SHALL save each board by itself, while the owner writes and when the owner leaves the board. There is no save key, and the app does not ask about unsaved changes.
3. Saving SHALL NOT interrupt writing. See section 6.5.
4. WHEN a board opens, THE SYSTEM SHALL show it as it was when last saved, at its last view.
5. All boards SHALL be stored on the owner's computer only.

#### 6.1.8 Tuning

1. Until the writing test passes, the tuning keys and the status bar with their values SHALL stay, so that the owner can see and change the values.
2. After the writing test passes, the tuning keys and the status bar SHALL be removed. The best values found become the fixed values.
3. THE SYSTEM SHALL then offer a simple, clean settings view that holds pen thickness and line height only.
4. After that, a stray key press SHALL NOT change how writing behaves.

### 6.2 Worked examples

| Input | Expected result | Notes |
| --- | --- | --- |
| "I", then go back left, then "am" | "I am": one word gap, no overlap | Short words stacked in an earlier spike version |
| "Oll", go back left, "ie" | "Ollie": one word | The jump is in the middle of a word |
| "Pythagoras" in three passes | One word, with letter gaps at the joins | Longer than the touchpad |
| "Hello", go back left, "world" | "Hello world": one word gap, no key press | See section 6.1.2.1 |
| "L", pause, "a", pause, "z", pause, "y", each to the right of the last | "Lazy": letters stay where written | Slow writing |
| "O" as the first letter of a word | Left edge of the "O" at the caret, word gap kept | Curves left from its first point |
| "T": upright, slow pause, crossbar | The crossbar is on the upright | Defects 2 and 4 in section 4.1 |
| "in", pause, then dot the "i" | The dot is on the "i" | Defect 2 in section 4.1 |
| "B": upright, then bowls that start far left | The bowls join the upright, and writing does not move along | Defect 3 in section 4.1 |
| "triangle" reaches the right edge of a column at the "g" | After the finger lifts, "trian" and the "g" move to a new line below, and writing continues there | Words stay whole |
| A word wider than the whole column | The word splits at the edge and continues on a new line | The one case where a word splits |
| Press Enter | New line below, at the same left end | |
| Press Space, then write | A word gap before the next stroke | Optional |
| Press Backspace after Enter, with no ink on the new line | Last stroke of the earlier line is removed. The caret is at the end of the remaining ink on that line | Defect 10 in section 4.1 |

#### 6.2.1 Writing test

The owner writes these lines at normal speed, with no key press except Enter between lines. Together they hold 41 words and every word length from 1 to 12 letters.

| Line | Text | Checks |
| --- | --- | --- |
| 1 | I am at a desk | 1, 2 and 4 letters |
| 2 | The lazy dog jumps over the fence | 3, 4 and 5 letters, round letters |
| 3 | Ollie is writing a square and a triangle | 5, 6, 7 and 8 letters |
| 4 | Pythagoras drew a rectangle on the whiteboard | 9 and 10 letters |
| 5 | Bright handwriting needs measurements | 6, 11 and 12 letters |
| 6 | Tim Jill Pat and Rob sit in a big tent | Capitals, dots and crossbars |

Each of these counts as one error:

| Error | Meaning |
| --- | --- |
| Split word | A gap inside a word looks like a word gap. |
| Joined words | Two words have no visible word gap. |
| Overlap | Ink covers other ink, so that a letter is hard to read. |
| Misplaced stroke | A dot, a crossbar or a part of a letter is not on its letter. |

The writing passes when both of these are true:

1. The owner makes no more than 2 errors in the 41 words, on two runs in a row.
2. The owner says that the writing feels free and easy.

The owner can change these numbers and the lines after trying the test.

### 6.3 States

| State | Meaning |
| --- | --- |
| Home screen | The app has started, or the owner left a board. No writing is possible. |
| Mouse mode | A board is open and Caps Lock is off. The touchpad is a normal mouse. |
| Writing mode | A board is open and active, and Caps Lock is on. The touchpad writes. |

### 6.4 User interface

#### 6.4.1 Home screen

Purpose: the first screen when the app starts. The owner manages saved boards here.

| Item | Behaviour |
| --- | --- |
| List | Shows every saved board, most recently changed first, with a small preview picture of its contents, its name and its last-changed time. |
| New board | The owner chooses a layout, and the board opens at once. |
| Name | A new board gets a name from the date and time. The owner is not asked to type one. |
| Open | Opens the board where the owner left it. |
| Rename | Changes the board's name. |
| Delete | Asks first, then removes the board. |

States:

* **Empty:** no saved boards. The screen shows the new board action and a short sentence that says what to do.
* **Error:** a board cannot be read. The list shows the board as damaged, and the other boards still work.

#### 6.4.2 Board screen

* The board fills as much of the screen as possible. Controls stay out of the way of writing.
* The owner can go back to the home screen with Caps Lock off.
* The app shows that it is in writing mode.

### 6.5 Non-functional requirements

The numbers are proposals for the owner to check.

1. **Writing response:** ink appears on the screen within 50 ms of the finger movement, and the owner sees no lag during the writing test.
2. **Large boards:** with 20,000 strokes on a board, writing response stays within item 1.
3. **Saving:** saving causes no delay that the owner can see during writing.
4. **Data safety:** if the app or laptop stops suddenly, the owner loses no more than the last 5 seconds of writing.
5. **Opening:** a board with 20,000 strokes opens within 2 seconds.
6. **One-handed use:** no daily action needs a key or button held down while the finger moves. Holding a button with the other hand was one of the problems in Paint (section 2.4).
7. **Few key presses:** writing a line of text needs no key press. Every other daily action needs no more than one key press or one click, except where this document says that the app asks first.
8. **Privacy:** the app makes no network connection.
9. **Platform:** Windows 11, on a laptop with a precision touchpad.

### 6.6 Edge cases

| Case | Expected result |
| --- | --- |
| No precision touchpad found | The board screen shows a clear message when the owner tries to write. |
| Caps Lock is on when the app starts | The home screen works as normal. Writing mode starts when a board opens, at the pointer. |
| A second finger touches during a stroke | No effect. The stroke continues with the first finger. |
| The touchpad never reports a finger up | The next touch starts a new stroke. |
| Tap-to-click while writing | No effect on the board. |
| Window stops being active during a stroke | The stroke ends and is kept. The cursor is free. |
| App closes in writing mode | The cursor is free and the board is kept. |
| The pointer is outside the board when Caps Lock is turned on | The new line starts at the nearest point on the board. |
| Backspace with no strokes | Nothing happens. |
| Backspace during a stroke | Nothing happens. |
| A board cannot be saved, for example the disk is full | The app shows a message, keeps the board open and tries again. |
| A board file is damaged | The home screen shows it as damaged. The app does not stop. |
| Two boards have the same name | Allowed. |
| The same board is opened twice | Not possible. One board has one open window. |
| A second copy of the app starts | The first copy comes to the front. No second copy runs. |
| The window changes size | The ink does not change. |
| The laptop sleeps or locks in writing mode | The same as when the window stops being active. The board is saved. |
| The window moves to a screen of another size | The board keeps its size and the ink does not change. New writing keeps the same size on the screen. |

---

## 7. Open questions

None. The owner settled every question on 4 November 2026, and the answers are in the sections above.

---

## 8. Assumptions

1. The owner is the only user, and the app runs on one laptop.
2. Writing starts from Caps Lock, as in the spike. The owner has not asked to change this.
3. The starting values in section 2.4 are good starting points. The writing work can change them.
4. The owner writes from left to right, in English.
5. A dot or crossbar for a letter in an earlier pass cannot be reached, because the touchpad maps only to the current writing window. The owner adds it before moving along.
6. Longer words can be solved with the whole touchpad mapping or with the row rule (section 6.1.2.1). If none of the methods in that section passes the writing test, this document changes.

---

## 9. Definition of done

* The owner passes the writing test in section 6.2.1, and the result is recorded.
* The worked examples in section 6.2 give the expected results.
* The owner can zoom and move around a large board, and writing stays on the screen while they write.
* Closing and opening the app keeps every board as it was.
* From the home screen, the owner can create, find, open, rename and delete boards.
* The owner can write on a blank board and on the three-column layout.
* The owner can write in each colour, and erase ink.
* Every number in section 6.5 is met or changed with the owner's approval.
* The owner has used the app for their own notes for 5 working days and has lost no work.
* The placement rules have automated tests, including the worked examples in section 6.2.
* No `[NEEDS CLARIFICATION]` remains in this document.
