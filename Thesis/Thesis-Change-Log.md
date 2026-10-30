# Thesis Change Log

Working lab notebook for the master’s thesis on the Spruce Beetle Grasshopper plugin. This is **not** a user-facing changelog.

Original plugin: Dominik Reisach, *Spruce Beetle*. This repo (`Spruce-Beetle-2.0`) is a working copy for thesis development.

---

## Active research focus

<!-- Update this when the research question or primary workflow changes. The agent reads this when writing Motivation. -->

Pack leftover rectangular offcuts into a **2′ × 2′ × 8′ (24″ × 24″ × 96″) column** with Bin Packing EB-AFIT (Rhino inches). **Stagger Column**, **Spine Column**, and **Bond Column** are alternate packers for that box: stagger neighboring ends, stand 1–4 long scraps on the floor first, or keep every wide face pointing one way and offset the seams in plan so the dowels join one body. Bin Packing EB-AFIT stays the comparison. Joints: **Packed Contacts → Select Contacts → Contact Tenon** (captured blind keys), **Contact Spline** (edge-open keys), or **Outside Key** (face keys on the column skin after the stack). **Dowel Column** ranks horizontal through-dowels on the packed stack (widest vertical face, diameter and clearance). The full-height vertical placer is hidden as Legacy Dowel Placer. Alignment Tenon / Spline stay on the curve tab. Packed Stacks was deleted. Spec: [Packing-Joints.md](../Documentation/Packing-Joints.md).

Related guides already in the repo:

- [Documentation/README.md](../Documentation/README.md) — what each doc is for
- [README.md](README.md) — what is in this Thesis folder
- [Timeline/Work-Timeline.md](Timeline/Work-Timeline.md) — dated order of the modeling work
- [Packing-Joints.md](../Documentation/Packing-Joints.md)
- [Component-Reference.md](../Documentation/Component-Reference.md)
- [Column-Fill-2x2x8.md](../Documentation/Column-Fill-2x2x8.md)
- [Grasshopper-Plugin-Dev-Guide.md](../Setup/Grasshopper-Plugin-Dev-Guide.md)

---

## How to log

1. Put the **newest entry at the top** of the Log section (reverse chronological).
2. One entry per coherent change (one feature, fix, experiment, or docs/setup batch).
3. Copy the template below; fill every field. Use `N/A` only when a field truly does not apply.
4. Types: `feature` | `fix` | `experiment` | `docs` | `setup`
5. Log failures and reverted approaches — they are thesis evidence.

### Entry template

```markdown
### YYYY-MM-DD — <type>: <one-sentence summary>

- **Motivation:** Which thesis question or workflow this serves (see Active research focus).
- **Files:** Paths touched.
- **Before → after:** Observable Grasshopper / plugin behavior.
- **Result / observation:** What we learned (including failures).
- **Follow-ups:** Next tests, docs, or open questions.
```

---

## Log

### 2026-10-30 — docs: Measured mixed-section stock from the clipboard notes

- **Motivation:** Column packing needs a real measured pile of mixed sections, not only generated test lengths.
- **Files:** `Documentation/TestData/stock_measured_mixed.csv`
- **Before → after:** File did not exist. 88 rows, `index;x;y;z` in inches, no header. Rows 1–7 are 1.5×7.125, 8–72 are 1.5×3.5, 73–84 are 0.625×2.625 (2⅝ × ⅝), 85–88 are 1.9375×7 (1 15/16 × 7). Sheet number 89 has no length and is omitted.
- **Result / observation:** Lengths are the close-up readings. Rows 4 and 5 are still 42.875 and 18.5; those two are the ones left for a hand edit.
- **Follow-ups:** Correct rows 4 and 5. Component-Reference does not need an update (test data only).

### 2026-10-29 — docs: One 44″ and one 60″ 2×4 added back to the column cut list

- **Motivation:** The user wanted a couple of longer parts in the otherwise ≤36″ pile.
- **Files:** `Documentation/TestData/stock_column_2x4_2x6.csv`
- **Before → after:** 69 parts, longest 36″ → 71 parts. New rows 64 (`1.5;3.5;44`) and 65 (`1.5;3.5;60`) sit after the 2×4s. The 2×6 rows moved from 64–69 to 66–71 with the same sizes. These two are added parts, not re-joined from the earlier cuts.
- **Result / observation:** Total 2×4 length is now 1609″ (was 1505″). The 2×6 total stays 192″. Volume is about 10,031 cubic inches. The 60″ part is the only piece longer than half the 96″ column height.
- **Follow-ups:** Same re-pack as the entry below. Component-Reference does not need an update (test data only).

### 2026-10-29 — docs: Column cut list capped at 36″ per part

- **Motivation:** The test pile for the 24 × 24 × 96 column should be made of shorter parts, with nothing longer than 36″.
- **Files:** `Documentation/TestData/stock_column_2x4_2x6.csv`
- **Before → after:** The file had 47 parts with 16 over 36″: 2×4s at 40, 42, 44, four at 48, 60, 64, two at 72, 78, and two at 84, plus two 2×6s at 96. Each one is now cut into two or three pieces of 12–36″ (for example 48 → 24 + 24, 78 → 36 + 30 + 12, 96 → 32 + 32 + 32 and 36 + 36 + 24). Parts at 36″ or shorter are unchanged. The file is now 69 parts: rows 1–63 are 2×4 (`1.5;3.5`), sorted shortest first, and rows 64–69 are 2×6 (`1.5;5.5`). Saw kerf is ignored.
- **Result / observation:** The longest part is 36″ and the shortest is still 12″. Total 2×4 length stays 1505″, total 2×6 length stays 192″, and volume stays 9,485 cubic inches, so this is the same lumber in shorter pieces. Nothing can run more than 36″ up the 96″ column now, so Spine Column has no long sticks to stand first. The pack results for this file in the 2026-10-28 Bond Column entry are for the old 47-part list.
- **Follow-ups:** Re-pack the 24 × 24 × 96 box with Bin Packing EB-AFIT, Stagger Column, and Bond Column and compare to the 47-part results. Component-Reference does not need an update (test data only).

### 2026-10-28 — docs: Examples.gh versions 27 and 28 filled in

- **Motivation:** The `Examples.gh` version table in the timeline had two rows marked "not listed".
- **Files:** `Thesis/Timeline/Work-Timeline.md`
- **Before → after:** Versions 27 and 28 were unknown. Version 27 is Sep 14, 10:53 PM, 24.2 KB; version 28 is Sep 19, 11:59 AM, 46.6 KB. The per-day table now reads Sep 14: versions 22–27, 11:30 AM – 10:53 PM; Sep 19: versions 28–34, 11:59 AM – 4:57 PM. The "Sep 14–19 not listed" row and the gap item asking for these two versions are removed.
- **Result / observation:** All 71 `Examples.gh` versions are now listed. No saves fall between Sep 14 and Sep 19.
- **Follow-ups:** `Dowel Model.3dm` and `.3dmbak` version history. Component-Reference does not need an update.

### 2026-10-28 — docs: Version-list screenshots removed from the timeline

- **Motivation:** The user sent the OneDrive version-list screenshots as a data source, not as images for the timeline.
- **Files:** `Thesis/Timeline/Work-Timeline.md`, `Thesis/Timeline/images/` (eight `*-versions.png` files deleted)
- **Before → after:** The timeline showed six screenshots of the `Examples.gh` version list and two for `Examples.3dm` and `Packed Tenon joints.gh`. Those images and their links are gone. The version tables, dates, sizes, and observations taken from them stay.
- **Result / observation:** 57 images remain, all linked, none unused.
- **Follow-ups:** None. Component-Reference does not need an update.

### 2026-10-28 — docs: File creation dates and two more OneDrive version lists in the timeline

- **Motivation:** The user sent version histories for `Packed Tenon joints.gh` and what was taken to be the dowel model, which showed only one version.
- **Files:** `Thesis/Timeline/Work-Timeline.md`, `Thesis/Timeline/images/2026-10-28_152755-examples-3dm-versions.png`, `Thesis/Timeline/images/2026-10-28_152809-packed-tenon-joints-gh-versions.png`
- **Before → after:** The snapshot table had only last-saved dates. It now has the Windows creation date for each file and the new version counts. A new "Other OneDrive version histories" table lists `Examples.3dm` (one version) and `Packed Tenon joints.gh` (two versions). The Oct 6 day entry notes when `Dowel Model.3dm` was created. The Examples.gh section no longer says the Aug 28 canvases were unsaved.
- **Result / observation:** The one-version history (308 KB, Sep 14 at 4:25 PM) is `Examples.3dm`, not `Dowel Model.3dm`, which is 68.8 MB and was last saved Oct 28 at 3:23:47 PM. `Dowel Model.3dm` and its `.3dmbak` were created Oct 6 at 11:00:39 AM, the same minute as `Examples.gh` version 53 and 14 minutes after the Oct 6 Rhino autosave, so that file holds work from Oct 6 on. `Initial_Tests.gh` and `.3dm` were created Aug 28 at 4:04–4:05 PM, right after that day's last screenshot, so the Aug 28 tests were saved there; the earlier timeline said the Aug 28 window was unnamed. `Examples.3dm` was created Sep 9 at 10:40 AM, the same minute as `Examples.gh` version 4. `Packed Tenon joints.gh` was created Sep 20, after its last save on Sep 19, so it was copied or downloaded onto this computer. Its two versions are Sep 19 at 2:48 PM (35.6 KB) and 3:11 PM (14.2 KB). Untested explanation for the short Rhino histories: a Rhino save with backup renames the old file to `.3dmbak` and writes a new `.3dm`, which OneDrive may treat as a new file. All 65 image links in the timeline resolve.
- **Follow-ups:** Check the version history on both `Dowel Model.3dm` and `Dowel Model.3dmbak`. Component-Reference does not need an update.

### 2026-10-28 — docs: Examples.gh OneDrive version history added to the timeline

- **Motivation:** OneDrive version history is the dated record of the Grasshopper definition. It shows which version to download to see the work as it was on a given day.
- **Files:** `Thesis/Timeline/Work-Timeline.md`, `Thesis/Timeline/images/` (six screenshots of the OneDrive version list, `2026-10-28_1526xx-examples-gh-versions.png` and `152703`)
- **Before → after:** The timeline listed OneDrive history as not pulled. It now has an `Examples.gh` section: a per-day table (versions, save times, sizes, the work logged that day), every version in a collapsible table, and the user's screenshots of the list. Gaps now ask for the `Dowel Model.3dm` history, the two missing `Examples.gh` rows, and a comparison of versions 54 and 55.
- **Result / observation:** 71 versions, from Sep 9 at 9:22 AM to Oct 28 at 3:23 PM. Versions 27 and 28 (between Sep 14 at 10:35 PM and Sep 19 at 12:16 PM) were not in the screenshots. Version 71 matches the local file (saved 3:23:40 PM, 511,168 bytes). Version 1 is from Sep 9, so the Aug 28 and Sep 2 canvases were not this file. Between version 54 (Oct 8, 3:08 PM, 40.3 KB) and version 55 (3:38 PM, 492 KB) the file grew by about 450 KB and stayed near 500 KB afterward; the cause is not known yet. Versions 21, 24, 69, and 70 share their minute with the saves of `Examples.3dmbak`, `Examples.3dm`, `Dowel Model.3dmbak`, and `Dowel Model.3dm`. All 63 image links in the timeline resolve.
- **Follow-ups:** Get the `Dowel Model.3dm` version history. Fill in versions 27 and 28. Open versions 54 and 55 to see what was added on Oct 8. Component-Reference does not need an update.

### 2026-10-28 — setup: Thesis-Planning renamed Thesis; change log and timeline moved into it

- **Motivation:** Thesis records (the lab notebook, the work timeline, the calendar, the proposal) were split between `Documentation/` and `Thesis-Planning/`. The user asked for them in one folder named `Thesis`.
- **Files:** `Thesis-Planning/` → `Thesis/` (git rename); `Documentation/Thesis-Change-Log.md` → `Thesis/Thesis-Change-Log.md` (git rename); `Documentation/Timeline/` → `Thesis/Timeline/` (was untracked); `Thesis/README.md`; `Documentation/README.md`; `README.md`; `.cursor/rules/thesis-change-log.mdc`
- **Before → after:** The change log lived at `Documentation/Thesis-Change-Log.md`, the timeline at `Documentation/Timeline/`, and the calendar and proposal in `Thesis-Planning/`. All three are now under `Thesis/`. The logging rule points at `Thesis/Thesis-Change-Log.md`. The links at the top of this log go to `../Documentation/` for the plugin docs. `Thesis/README.md` lists the log and the timeline with the calendar files. `Documentation/README.md` and the root `README.md` link to the `Thesis` folder instead of the old paths. Plugin docs stay in `Documentation/`: Component-Reference, Packing-Joints, Column-Fill, TestData, Examples, Reproduce. File paths in older entries below are left as they were written.
- **Result / observation:** A link check over every Markdown file in `Thesis/`, `Documentation/README.md`, and the root `README.md` found no broken links. No other file outside this log mentions `Thesis-Planning`, `Documentation/Thesis-Change-Log`, or `Documentation/Timeline`.
- **Follow-ups:** If `Packing-Joints.md` is treated as thesis writing rather than a plugin spec, it can move too. Component-Reference does not need an update.

### 2026-10-28 — docs: Timeline screenshots carry the issue, discussion, and outcome from each chat

- **Motivation:** The first timeline captions only described what was on screen. Showing the work needs the problem each screenshot was sent with and what came of it.
- **Files:** `Documentation/Timeline/Work-Timeline.md`, `Documentation/Timeline/images/` (now 57 PNGs)
- **Before → after:** Each day's table of one-line captions became one block per chat message. A block shows the screenshot or screenshots sent together, then what they show, the question or problem raised, what the reply found, and the outcome (taken from the change log when the chat reply ended before the fix), plus the chat ID. Fifteen screenshots that were missing were added from Cursor's workspace image folder: nine from the Sep 2 axis-mapping session, three from the Sep 25 outside-key discussion, and one each from Oct 12, Oct 13, and Oct 28. Every existing screenshot was swapped for the original from that folder, because the copies in the chat assets folder had been recompressed. Four `-pasted` names moved to the earlier time the image was pasted into the chat box (Aug 28 14:57 → 14:56, Oct 8 15:40 → 15:38, Oct 9 14:11 → 13:38, Oct 16 11:18 → 11:03). One Oct 9 image that repeated `133435` without the blue mark and was never sent was dropped.
- **Result / observation:** The Aug 28, 3:00 PM screenshot was a Logison CSV 2 Box definition, not a Spruce Beetle pack; the first timeline had captioned it as the first Bin Packing run. The Sep 2 session shows the path to the Z-up fix: the input box taken from a side face, beginner questions about Domain and Domain Box, a reply that wrongly assumed a Brep on Domain Box `B`, and then the Y-up library mapping found at 3:24 PM and fixed in code at 10:11 PM. All 57 image links in the page resolve, and no file in `images/` is unused.
- **Follow-ups:** Same as the earlier timeline entry: OneDrive version history, "not before" dates for the columns in `Dowel Model.3dm`, and checking `RhinoAutosave.3dm`. Component-Reference does not need an update.

### 2026-10-28 — docs: Reconstructed work timeline with dated screenshots

- **Motivation:** A month of iterations went into one Rhino file (`Dowel Model.3dm`) and one Grasshopper file (`Examples.gh`), saved over each other. Neither file records when an object was made. The order of the work is needed to show it in the thesis.
- **Files:** `Documentation/Timeline/Work-Timeline.md`, `Documentation/Timeline/images/` (43 PNGs), `Documentation/README.md`
- **Before → after:** No timeline existed. The new page lists the dated sources: change log entries, git commits, screenshots, snapshot files on OneDrive and in the Rhino and Grasshopper autosave folders, and a table giving the earliest day each visible feature could appear on a column. The day-by-day section runs from Aug 27 to Oct 28 with captions. Screenshots were copied from the Cursor chat image folder and renamed by date. Names with six digits keep the capture time from the original filename; names ending in `-pasted` use the time the image was pasted into chat. Three duplicate pastes were dropped. Five images that are not model work were left out: Fusion 360 preferences, two Rhino options dialogs, an Amazon product page, and a blank March 2027 calendar template.
- **Result / observation:** The current `Dowel Model.3dm` and `Examples.gh` were last saved Oct 28, 12:45 PM. Older snapshots exist from Sep 8 (`Initial_Tests`), Sep 13–14 (`Examples.3dm` and its backup), Sep 19 (`Packed Tenon joints.gh`), Oct 2 (`Examples_autosave.gh`), Oct 6 (`RhinoAutosave.3dm`), and Oct 27 (`Dowel Model.3dmbak`). Screenshots cover Aug 28, Sep 2, 9, 23, Oct 2, 5, 6, 8, 9, 12, 16, and 28. The Aug 28 and Sep 2 screenshots show packed pieces lying in a row beside the column, before the Sep 2 evening fix to container size and Z-up mapping. OneDrive version history was not checked.
- **Follow-ups:** Pull the OneDrive version history for `Dowel Model.3dm` and `Examples.gh` and keep one copy per working day. Give each column in the current file a "not before" date from the feature table. Open a copy of `RhinoAutosave.3dm` to see which file it was. Component-Reference does not need an update.

### 2026-10-28 — feature: Bond Column packs a running bond in plan so dowels join the whole column

- **Motivation:** Plan views of packed columns showed two or more bodies with no connection. Dowel Column only joins boards through their wide faces. The locked packers turn every board the same way, and in a grid of one width the seams inside a layer run straight through every layer. Boards across such a seam only meet edge to edge, so no dowel crosses it. In elevation the ends were already staggered well. The same brick bond was needed in plan.
- **Files:** `Packing/BondPacker.cs`, `Packing/BondColumn_GH.cs`, `Documentation/Component-Reference.md`, `Documentation/Column-Fill-2x2x8.md`
- **Before → after:** Cause in `AxisLockPacker.FindPose`: the turned footprint is only kept when it sits strictly lower, and ties go to the lowest X, then the lowest Z. Every board kept its first orientation on the same grid, so the seams lined up. New **Bond Column** (`BondCol`, GUID `B4C71E28-5D93-4A6F-8E12-7F3A0D9C6B41`) keeps every thickness on one plan axis, so every wide face points the same way. A board's side edges stay at least `Bo` (default 1.5) from every seam in a layer it touches face to face. Candidate spots include centered on a seam and `Bo` either side of it. The edge gap the offset leaves is filled by a narrower scrap when one fits, never by a turned board. Ends stagger by `St` (default 4) as in Stagger Column, and nothing bridges a hole. It packs with thickness along X and along Y and keeps the larger volume. The remark gives the axis, the number of groups joined through wide faces (contact at least `Bo` across and in height), the number of boards still next to a seam, and the number of aligned ends. It warns when there is more than one group. Stagger, Spine, and Bin Packing are unchanged.
- **Result / observation:** `BondPacker.Check()` passed with 0 failures, run by reflection on the built assembly outside Grasshopper. A 7 × 7.5 × 48 box of 2×4s forms one group with no board next to a seam. With `Bo = 0` the same pile packs a grid and splits into more than one group. A 24 × 24 × 96 column of 260 2×4s forms one group. In a 7 × 7.5 box, 1.5 × 1.5 scraps fill the wall gaps beside the offset boards. The table below compares the same stock with `St = 4` and contact `Bo = 1.5`; groups for Stagger were counted with the same rule.

  | Stock and box | Stagger packed / volume / groups | Bond packed / volume / groups |
  | --- | --- | --- |
  | `stock_column_2x4_2x6.csv`, 7 × 7.5 × 96 | 12 / 4,059 / 8 | 12 / 4,059 / 1 |
  | `stock_column_2x4_2x6.csv`, 24 × 24 × 96 | 47 / 9,485 / 6 | 47 / 9,485 / 1 (12 boards next to a seam) |
  | `stock_3.5x1.5_lengths_250.csv`, 7 × 7.5 × 96 | 10 / 3,954 / 2 | 12 / 3,966 / 1 |
  | `stock_3.5x1.5_lengths_250.csv`, 24 × 24 × 96 | 175 / 51,098 / 12 | 150 / 45,187 / 1 |
  | `parts_varied_250.csv`, 24 × 24 × 96 | 73 / 42,370 / 18 | 83 / 46,874 / 1 |

  The price of the bond shows in the full 2×4 column: 82% filled against 92%. Stagger turns boards to fill the 3 inch strip left at the wall (24 is not a multiple of 3.5). Bond Column will not, because a turned board would meet its neighbors edge on. In the 24 × 24 case with only 47 parts, the partly filled floor ran out of offset spots and 12 boards went beside a seam; it was still one group. Plan sections of the 7 × 7.5 pack showed the 2×6s and the 2×4 pairs alternating with offset seams. Run time on the 250-part piles was 0.8 s and 2.1 s for Bond, against 44 s and 108 s for Stagger. MSBuild Debug compiled. The copy to `bin/` was blocked because Rhino 8 (PID 36852) holds `SpruceBeetle.gha`; the built assembly is in `obj/Debug/net48/`. The Grasshopper viewport was not opened.
- **Follow-ups:** Close Rhino, rebuild, reload Grasshopper, and wire the same CSV and box into Stagger Column and Bond Column. Feed Bond Column `Oc` into Dowel Column and confirm that the rods cross the offset seams. The group count could also go on Dowel Column so every packer reports it. Component-Reference and the column-fill tweak table are updated.

### 2026-10-27 — docs: Drop full-length 2×4s from the bought-lumber cut list

- **Motivation:** An uncut 96″ 2×4 is a full board, so it would not show up as a scrap in this pile.
- **Files:** `Documentation/TestData/stock_column_2x4_2x6.csv`
- **Before → after:** Rows 46–48 were `1.5;3.5;96`. Those three rows are gone. The file is 47 parts. Longest 2×4 is 84″ (two of them). The two bought 2×6 × 8′ boards stay as rows 46–47, `1.5;5.5;96`. Combined 2×4 length is 1505″ (about 125.4 ft).
- **Result / observation:** Seven pieces are still at least 72″: two 72″, one 78″, two 84″, and the two 96″ 2×6s. Volume is about 9,500 cubic inches.
- **Follow-ups:** Component-Reference does not need an update (test data only).

### 2026-10-27 — docs: Column cut list from bought 2×4s and two 2×6×8′ boards

- **Motivation:** The next column pack should use the lumber actually purchased: dressed 2×4 (1.5″ × 3.5″) plus two 2×6 × 8′ boards, cut into parts long enough to handle and with a few sticks long enough to run up the 96″ column.
- **Files:** `Documentation/TestData/stock_column_2x4_2x6.csv`
- **Before → after:** That file did not exist. It is 50 rows, `index;x;y;z` in inches, no header. Rows 1–48 are `1.5;3.5` with lengths from 12″ to 96″ (shortest first). Nothing is under 12″. Eight of those are at least 72″ (3/4 of the column height): two at 72″, one at 78″, two at 84″, three at 96″. Rows 49–50 are the two bought boards left full length, dressed 2×6 as `1.5;5.5;96`. Combined 2×4 length is 1793″ (about 149.4 ft).
- **Result / observation:** 50 parts, minimum length 12″, ten pieces at 72″ or longer including the two 2×6s. Volume is about 11,000 cubic inches against the 55,296 cubic inch column, so this pile cannot fill the box solid.
- **Follow-ups:** Point CSV to Offcut at this file and pack the 24×24×96 box. Spine Column can take the 96″ sticks first. Component-Reference does not need an update (test data only).

### 2026-10-26 — docs: Weekday boxes wider than weekend boxes

- **Motivation:** Handwriting happens on work days. Equal seven-column boxes made Monday–Friday narrower than they need to be.
- **Files:** `Thesis-Planning/Printable-Calendar.html`, `Thesis-Planning/Printable-Calendar.pdf`
- **Before → after:** Sunday through Saturday shared the row equally, and a month with six weeks had shorter boxes than a month with five. Monday–Friday are now the same width and the same height on every page. Saturday and Sunday are narrower. Each week row is 1.14 inches tall.
- **Result / observation:** PDF is still letter landscape, 8 pages. Checked after reprint.
- **Follow-ups:** Component-Reference does not need an update.

### 2026-10-26 — docs: Printable calendar switched to landscape

- **Motivation:** The month grid was portrait. Landscape gives a wider box to write in.
- **Files:** `Thesis-Planning/Printable-Calendar.html`, `Thesis-Planning/Printable-Calendar.pdf`, `Thesis-Planning/README.md`
- **Before → after:** Each page was letter portrait (8.5 by 11 inches). Each page is now letter landscape (11 by 8.5 inches). The dates in the boxes are unchanged.
- **Result / observation:** The reprinted PDF MediaBox is 792 by 612 points, which is letter landscape, and it is still 8 pages.
- **Follow-ups:** Component-Reference does not need an update.

### 2026-10-26 — docs: Printable monthly calendar for handwriting

- **Motivation:** The date list and the Outlook file are not a page you can print and write on.
- **Files:** `Thesis-Planning/Printable-Calendar.html`, `Thesis-Planning/Printable-Calendar.pdf`, `Thesis-Planning/README.md`, `Thesis-Planning/Calendar.md`
- **Before → after:** No printable grid. Added eight letter-size months, October 2026 through May 2027, with LSU A&M employee holidays and the thesis deadlines in the day boxes. The rest of each box is blank.
- **Result / observation:** Checked October, December, January, and March in the browser. October 26 is Monday. December 24–25 and 28–31 are winter holiday. January 21–22 hold the add and forms deadlines. March 1 is Monday, March 25 is the upload, and March 26 is Good Friday. The PDF is 8 pages.
- **Follow-ups:** Component-Reference does not need an update.

### 2026-10-26 — docs: Outlook calendar file keeps employee holidays and thesis deadlines

- **Motivation:** The import file also carried student breaks. Those days do not change the employee work schedule.
- **Files:** `Thesis-Planning/Spring-2027-Thesis.ics`, `Thesis-Planning/README.md`, `Thesis-Planning/Calendar.md`
- **Before → after:** The `.ics` included student Thanksgiving, fall finals, the start of spring classes, the student Mardi Gras span, and spring break. Those events are removed. Employee closures and the Graduate School deadlines remain.
- **Result / observation:** Re-import replaces same-UID events. Events that were deleted from the file stay on a calendar that was already imported; those have to be deleted by hand.
- **Follow-ups:** Component-Reference does not need an update.

### 2026-10-26 — docs: Committee step is the defense form, not a fall appointment form

- **Motivation:** The planning checklist told the student to get committee approval in the fall without saying which form, if any, the Graduate School actually uses.
- **Files:** `Thesis-Planning/Calendar.md`
- **Before → after:** The checklist said to confirm the committee and finish outside-member approval this fall. It now says an email agreement is enough until the Request for Master’s Defense and Degree Audit, and a CV is added only for someone who is not LSU graduate faculty.
- **Result / observation:** The current Graduate School forms list has no separate committee-appointment form. The defense-and-audit form is what names the committee. Department practice can still be earlier.
- **Follow-ups:** Confirm with the graduate coordinator whether the department keeps its own committee sheet. Component-Reference does not need an update.

### 2026-10-26 — docs: Spring 2027 thesis calendar and planning folder

- **Motivation:** Graduation paperwork and the writing schedule have to sit on real days off. LSU A&M employee holidays are a different list from the student academic calendar.
- **Files:** `Thesis-Planning/README.md`, `Thesis-Planning/Calendar.md`, `Thesis-Planning/Spring-2027-Thesis.ics`, `Thesis-Planning/Proposal/README.md`, `Thesis-Planning/Submissions/README.md`, `Documentation/README.md`
- **Before → after:** The repo had no graduation calendar. It now has the Graduate School deadlines for a Spring 2027 thesis, the FY 2026–27 LSU A&M employee holidays (PM-5), and the student breaks that still count as work days, plus an Outlook import file and folders for the proposal and submission confirmations.
- **Result / observation:** Application for Degree has been open since September 5, 2026. Hard dates are January 21 (add 8000), January 22 (degree application and defense request, 4:30 p.m.), March 25 (upload, 4:30 p.m.), and April 16 (editor corrections, 4:30 p.m.). Employee winter closure is December 24 through January 1, return January 4. Mardi Gras day off is Tuesday, February 9 only. Spring break (March 8–12) is a class holiday; PM-5 still schedules employees to work. Research and writing milestones are not assigned yet.
- **Follow-ups:** Add the project proposal under `Thesis-Planning/Proposal/`. Confirm any earlier departmental deadline with the graduate coordinator. Build the research and writing schedule against the work-day windows in `Calendar.md`. Component-Reference does not need an update.

### 2026-10-16 — feature: Stagger Column and Spine Column packers

- **Motivation:** Similar scrap lengths end on one plane, so a packed column splits into bands that only meet end to end.
- **Files:** `Packing/AxisLockPacker.cs`, `Packing/PackedOffcuts.cs`, `Packing/BinPackingCS_GH.cs`, `Packing/StaggerColumn_GH.cs`, `Packing/SpineColumn_GH.cs`, `Documentation/Component-Reference.md`, `Documentation/Column-Fill-2x2x8.md`
- **Before → after:** Longest-first locked packing put the longest remaining scrap at the lowest supported height, so equal lengths shared a top. **Stagger Column** (`StaggerCol`, GUID `6F2A9C14-8B37-4E51-A6D0-3C91F7E24B58`) still fills that lowest height, and there takes the longest scrap whose top misses every side neighbor by at least `St` (default 4). When none can, it takes the largest gap, then the longest. **Spine Column** (`SpineCol`, GUID `2D8E5B70-1A64-4C93-9F28-6E0B4A7D15C3`) stands `N` scraps on the floor first (default 2, clamped to 1–4), spread to opposite corners, then fills the rest longest-first. A spine should reach 3/4 of the column height. Shorter scraps are used when the pile has fewer, with a warning that lists those lengths. Both lock the longest side on Z, still refuse a piece that would bridge a hole, and output `POc`, `C`, and `Oc`. The item-to-Offcut solid loop moved into `PackedOffcuts`; Bin Packing’s own placement is unchanged.
- **Result / observation:** On a 6×2×80 row, longest-first placed 40 beside 40 beside 20. Stagger placed 40, 20, 40 and reported 0 side joints within 4. On an 8×2×96 row, three 24s and one 48: the 48 stands on the floor and its top is 24 past the neighbor. Spine `N = 1` places that 48 first and warns that 48 is short of 72. Spine `N = 2` puts the 48 at the origin and the next scrap at the opposite corner. Two equal 24s still pack, and the remark says the ends could not stagger. MSBuild Debug succeeded (only the pre-existing `CS0472` warnings). The Grasshopper viewport was not opened.
- **Follow-ups:** Reload Grasshopper. Wire the same CSV into PackBin, Stagger Column, and Spine Column. Component-Reference and the column-fill tweak table are updated.

### 2026-10-16 — docs: 250 varied parts and 250 lengths of 1.5×3.5 stock

- **Motivation:** Cut lists for CSV to Offcut: boards that stay at least 3″ wide and at most 3″ thick, plus a separate pile of dressed 2×4 (1.5″ × 3.5″) with a different length on every row.
- **Files:** `Documentation/TestData/parts_varied_250.csv`, `Documentation/TestData/stock_3.5x1.5_lengths_250.csv`
- **Before → after:** Those two files did not exist. Both use `index;x;y;z` in inches, no header, same layout as the other test CSVs. Parts: x is thickness (0.75″–3″), y is width (3″–11.75″), z is length (12″–93″); all 250 size triples are unique. Stock: every row is `1.5;3.5` (x = thickness, y = width, matching `stock_2x4_scraps.csv`) with a unique z from 8″ through 96″.
- **Result / observation:** Checked in Python: 250 part rows, none under 3″ wide, none over 3″ thick; 250 stock rows, 250 distinct lengths, longest is 96″.
- **Follow-ups:** Component-Reference does not need an update (test data only). Reload is not required; point CSV to Offcut at either path.

### 2026-10-13 — feature: Dowel Column repeats a rod along Z at a spacing

- **Motivation:** A tall board was getting one rod at the center of its patch and another from a different patch near the top. The same sideways center should be able to carry more rods at a chosen spacing.
- **Files:** `Packing/ColumnDowels.cs`, `Packing/ColumnDowels_GH.cs`, `Documentation/Component-Reference.md`
- **Before → after:** Each kept rod was a single height. New input `S` (default `0`, appended after `Contacts` so existing wires stay put) adds rods at that center ± `S`, `2S`, and so on. A station is skipped when the hole does not fit or the centerline is closer than one hole diameter to a rod already kept. These repeats do not consume `N`. `0` adds none.
- **Result / observation:** Box checks passed. Three boards from Z = 0 to 72 with `S = 24` keep rods at 12, 36, and 60. `S = 35.8` leaves only the center rod; the stations fall inside the edge gap.
- **Follow-ups:** Debug rebuild copied `SpruceBeetle.gha` to `bin/Debug/net48/`. Reload Grasshopper. On a tall column, set `S` and confirm extra rods share the sideways center of an existing rod.

### 2026-10-12 — feature: Dowel Column passes unjoined contacts to a smaller dowel

- **Motivation:** A diameter that does not fit a pair should hand that pair to another Dowel Column, the same way Contact Tenon hands skipped contacts to a smaller tenon.
- **Files:** `Packing/ColumnDowels.cs`, `Packing/ColumnDowels_GH.cs`, `Documentation/Component-Reference.md`
- **Before → after:** Dowel Column had no contact input or skip output. Optional `C` takes Packed Contacts, or `SkC` from a previous Dowel Column. When `C` is wired, only those pairs are joined. Pairs this diameter does not join come out as `Sk` (planes, preview only) and `SkC`. Wire `SkC` to a second Dowel Column `C` with a smaller `Dia` and the same Offcuts. Vertical contacts are ignored.
- **Result / observation:** Box check passed. Two separate stacks with only the second pair supplied dowel that pair and leave the first stack alone.
- **Follow-ups:** Debug rebuild copied `SpruceBeetle.gha` to `bin/Debug/net48/`. Reload Grasshopper. Wire Packed Contacts into the first Dowel Column `C`, then `SkC` into a second Dowel Column with a smaller `Dia` and the same offcuts.

### 2026-10-09 — fix: dowel clearance is a separate hole cutter

- **Motivation:** Dowel Column only builds cylinders. The solid difference was using those cylinders, so the hole matched the dowel and `Cl` never reached the cut.
- **Files:** `Packing/ColumnDowels_GH.cs`, `Documentation/Component-Reference.md`
- **Before → after:** Output `D` is the dowel at `Dia`, and that was the only solid. New output `H` is the same cylinder at `Dia + 2 * Cl`. The open-end test uses the hole radius, so the fatter cutter does not stick into a neighboring board.
- **Result / observation:** No box check; this is only the Grasshopper solids. A difference of `D` is still a tight hole.
- **Follow-ups:** Debug rebuild copied `SpruceBeetle.gha` to `bin/Debug/net48/`. Reload Grasshopper. Wire `H` into the solid difference. Keep `D` as the pin.

### 2026-10-09 — fix: dowel stick-out only on an open end

- **Motivation:** A half-diameter stub past the last board was entering the next board through its narrow edge.
- **Files:** `Packing/ColumnDowels.cs`, `Packing/ColumnDowels_GH.cs`, `Documentation/Component-Reference.md`
- **Before → after:** Both ends of every rod extended half a diameter past the outer faces. An end now extends only when that stub misses every other board. If the stub would enter another board, including through a narrow edge, that end stays flush with the face.
- **Result / observation:** Box check passed. A rod from X = 0 to 2 with a narrow-edge board starting at X = 2 keeps the X = 0 end extended and the X = 2 end flush.
- **Follow-ups:** Debug rebuild copied `SpruceBeetle.gha` to `bin/Debug/net48/`. Reload Grasshopper. An open end should stick out by half a diameter. An end aimed at another board should stay flush.

### 2026-10-09 — fix: dowels stick out half a diameter past the end boards

- **Motivation:** The rods stopped flush with the outer faces, so the ends disappeared into the wood.
- **Files:** `Packing/ColumnDowels_GH.cs`, `Documentation/Component-Reference.md`
- **Before → after:** Cylinder and axis ran from the outer face of the first board to the outer face of the last. Each end now continues half a dowel diameter past those faces. Which boards a rod pierces is unchanged.
- **Result / observation:** Placement checks were not re-run; the extension is only on the built curve and cylinder.
- **Follow-ups:** Rebuild once Rhino releases the `.gha`. The ends should show past the first and last board.

### 2026-10-09 — fix: extra dowels attach every board the first lines miss

- **Motivation:** A dowel should run through as many boards as it can, and every board that can share one should end up on a rod.
- **Files:** `Packing/ColumnDowels.cs`, `Packing/ColumnDowels_GH.cs`, `Documentation/Component-Reference.md`
- **Before → after:** `N` stopped the list. Boards outside those lines stayed loose unless a short tie-in could reach a neighbor through its thickness. After the `N` lines, the line through the most still-loose boards is added, then the next, until no such line fits. A line that would hit a kept dowel is skipped. Tie-ins still catch a board that cannot share a full line.
- **Result / observation:** Box checks passed. `N = 1` on three separate pairs keeps the first pair, then adds a dowel through each of the other two, so all six boards are on a rod. A loose board that shares a thickness with a neighbor is attached by that shared line instead of a short tie. The wide board still gets no dowel across its width.
- **Follow-ups:** Debug rebuild copied `SpruceBeetle.gha` to `bin/Debug/net48/`. Reload Grasshopper. Boards the first `N` lines miss should each land on another rod, the one through the most boards still loose.

### 2026-10-09 — fix: dowels stay a set distance off the board edge, and the top rod only steps to the next board

- **Motivation:** After the height nudge, the top rod left the top of the column, and the rods that remained sat flush with a board edge. The rod should stay one dowel diameter off every edge, and that distance should be an input. The top rod should only move far enough to enter one more board.
- **Files:** `Packing/ColumnDowels.cs`, `Packing/ColumnDowels_GH.cs`, `Documentation/Component-Reference.md`
- **Before → after:** A line moved to the height that pierced the most boards, which pulled the top rod down onto a lower edge. The hole only had to fit, so the cylinder could touch the board edge. New input `E` (default `1`) is the wood outside the dowel, in dowel diameters. The center stays `Dia / 2 + E × Dia` inside every board. A line now steps only to the nearest height that adds a board.
- **Result / observation:** Box checks passed. With `E = 0`, the high line still drops from Z = 18 to Z = 15.75. With `E = 1` and a 0.5 dowel it drops to Z = 15.25, one diameter of wood inside the next board. A 1 inch face holds a 0.5 dowel at `E = 0` and drops it at `E = 1`.
- **Follow-ups:** Debug rebuild copied `SpruceBeetle.gha` to `bin/Debug/net48/`. Reload Grasshopper. New input is `E`, default `1`. A rod should remain near the top, one diameter below the next board's top, and the lower rods should sit off the edges.

### 2026-10-09 — fix: a high dowel drops just far enough to enter the next board

- **Motivation:** The top rod in the column sits a few inches above one more board. It should drop into that board and keep piercing the boards it already crosses. Moving it to the center of the whole overlap was the wrong distance.
- **Files:** `Packing/ColumnDowels.cs`, `Packing/ColumnDowels_GH.cs`, `Documentation/Component-Reference.md`
- **Before → after:** Each line stayed at the center of its own patch, so a band above a slightly shorter board never entered that board. The half-area ranking from the previous entry was reverted; it would have sent the rod to the middle of the tall patch. A line now shifts to the nearest height that adds a board, and only when every board already on the line still contains the hole.
- **Result / observation:** Box checks passed. Two boards to Z = 20 and one to Z = 16: the line centered at Z = 18 moves to Z = 15.75 and picks up the shorter board. The line at the center of the full overlap stays at Z = 8.
- **Follow-ups:** Debug rebuild copied `SpruceBeetle.gha` to `bin/Debug/net48/`. Reload Grasshopper. The top dowel should drop a few inches into the next board; the lower dowels should stay.

### 2026-10-09 — fix: Dowel Column keeps the large shared overlap instead of a thin band above it

- **Motivation:** The rod was landing above the overlap of the stacked parts. A thin band there threaded more pieces, so it outranked the tall patch the parts actually share.
- **Files:** `Packing/ColumnDowels.cs`, `Packing/ColumnDowels_GH.cs`, `Documentation/Component-Reference.md`
- **Before → after:** Lines were ordered by piece count. The center of a thin band above the stack won when that band included extra scraps. The largest shared patch now wins when the other patch is less than half its area and sits in the same cross-section. A tie-in into that thin band is skipped too. The rod is the center of the large patch.
- **Result / observation:** Box checks passed, including a stack whose thin top band threads 6 pieces and whose tall overlap threads 4. The kept dowel is at the center of the tall overlap (Z = 8), and nothing is left in the band above it.
- **Follow-ups:** Close Rhino and rebuild so the copy lands in `bin/`. On the column, the dowel above the orange overlap should move to the center of that overlap.

### 2026-10-09 — fix: a tie-in dowel no longer bores across a board's width

- **Motivation:** Column dowels are drilled through the wide face. A leftover piece was still allowed to enter an already-doweled board on the narrow edge and run the full width, so a wide board got a hole from short edge to short edge.
- **Files:** `Packing/ColumnDowels.cs`, `Packing/ColumnDowels_GH.cs`, `Documentation/Component-Reference.md`
- **Before → after:** After the ranked lines, a loose piece could tie into a pinned neighbor along that neighbor's middle dimension and stop at the far side. A 1×4×8 stud was bored through the 4″ side. A wide board was bored through its width. The pinned piece now has to be thin on the same axis as the loose piece. The hole in it is the thickness, and a wide board on the other axis is left out.
- **Result / observation:** Box checks passed. The old 4″ edge tie is gone. A loose 1″ piece against the overhang of a pinned 1″ piece gets a 1″ tie. An 18″-wide board beside them is not on any dowel. Debug compile succeeded (only the pre-existing `CS0472` warnings). Copy to `bin/` was blocked because Rhino 8 (PID 42916) holds `SpruceBeetle.gha`. The built assembly is in `obj/Debug/net48/`.
- **Follow-ups:** Close Rhino and rebuild so the copy lands in `bin/`. On the column, the long rod through the wide board's short edge should disappear.

### 2026-10-08 — fix: bake skips joint and dowel axis lines; dowels run both ways and can stop on a short side

- **Motivation:** Baking a joint was writing the drive line into the document along with the solids. On the column, every kept dowel was the same direction and ran the full chain, so a side stack with no dowel of its own stayed loose.
- **Files:** `Packing/ContactTenon_GH.cs`, `Packing/ContactSpline_GH.cs`, `Packing/OutsideKey_GH.cs`, `Packing/ColumnDowels.cs`, `Packing/ColumnDowels_GH.cs`, `Documentation/Component-Reference.md`, `Documentation/Packing-Joints.md`
- **Before → after:** Component Bake skipped planes and still baked `Dir` on Contact Spline and Outside Key, and `Ln` on Dowel Column. Those lines and curves are preview only now. Contact Tenon has no axis curve; its bake skips lines too, so a later curve output would not land in the document. Dowel count `N` is spent across both horizontal axes: the best line, then the best line on the other axis, and so on. After that, a piece with no dowel gets a shorter rod through its wide face that stops at the far short side of the first already-doweled piece it meets, and does not continue through the rest of the column.
- **Result / observation:** Box checks outside Grasshopper: the earlier cases still pass. `N = 2` on an X run of 3 and a Y run of 2 keeps one dowel each way. A loose piece against the short side of a pinned pair gets one extra dowel from the outer face of the loose piece to the far short side of the pinned piece, and a further piece beyond that short side is not included. Debug compile succeeded (only the pre-existing `CS0472` warnings). Copy to `bin/` was blocked because Rhino 8 (PID 29124) holds `SpruceBeetle.gha`. The built assembly is in `obj/Debug/net48/`.
- **Follow-ups:** Close Rhino and rebuild so the copy lands in `bin/`. Bake Contact Spline, Outside Key, and Dowel Column: solids should appear, axis lines should not. On the column, the undoweled side should grow a dowel that stops on the short side of a piece that already has one.

### 2026-10-08 — fix: Dowel Column does not add a clearance slider

- **Motivation:** Clearance stays an input, but the canvas should not grow a slider every time Dowel Column is placed.
- **Files:** `Packing/ColumnDowels_GH.cs`, `Documentation/Component-Reference.md`
- **Before → after:** `BeforeSolveInstance` called `ClearanceSlider.Ensure`, which dropped a `0.001`–`0.01` slider onto an unwired `Cl` pin. That call is gone. `Cl` still defaults to `0.005`, and a negative value is still raised to 0. A slider already wired on a canvas stays until it is deleted.
- **Result / observation:** Other joint components still auto-add their clearance sliders. This change is only Dowel Column. Debug compile succeeded (only the pre-existing `CS0472` warnings). Copy to `bin/` was blocked because Rhino 8 (PID 29124) holds `SpruceBeetle.gha`. The built assembly is in `obj/Debug/net48/`.
- **Follow-ups:** Close Rhino and rebuild so the copy lands in `bin/`. Delete any clearance slider already connected to Dowel Column if the pin should sit at the default.

### 2026-10-08 — feature: Dowel Column ranks horizontal dowels through a packed stack

- **Motivation:** The column is packed first. Dowels are then cylindrical rods perpendicular to world Z, plunged through the widest face so a 3-axis router can drill them with that face on the bed. The earlier full-height vertical placer is not the working path.
- **Files:** `Packing/ColumnDowels.cs`, `Packing/ColumnDowels_GH.cs`, `Packing/DowelColumn_GH.cs`, `Documentation/Component-Reference.md`, `Documentation/Packing-Joints.md`
- **Before → after:** **Dowel Column** (`DowelCol`, GUID `C4A91E72-6B38-4F05-9D14-2E7B8A0C5F61`) placed stock onto full-height vertical rods and ignored an existing pack. That component is now hidden and retitled **Legacy Dowel Placer** (`DowelPlace`); its GUID is unchanged. The packing tab's **Dowel Column** is a new component (GUID `E8C4B1A6-3D72-4F58-9A14-7B6E0C5D2F93`, Find Intersections icon). Inputs: packed `Oc`, count `N` (default 4), diameter `Dia` (default 0.5), clearance `Cl` (default 0.005, same slider as Contact Tenon). A dowel axis is X or Y only, and only when that axis is normal to the widest face. Pieces do not have to touch; the cylinder includes empty spans and ends on the outer faces. The hole used for fit and spacing is `Dia + 2 * Cl`. The preview cylinder is `Dia`. Lines are ranked by how many pieces they pierce. `N` keeps that many, one per footprint, and skips a lower line whose hole would come closer than one hole-diameter to a kept hole. Outputs are cylinders, axes, scores, and a tree of packed indices. It does not cut the offcuts.
- **Result / observation:** Box checks outside Grasshopper passed: three boards stacked on their thickness give one horizontal dowel of score 3; a gap between the second and third stays on that rod and the span includes the air; the same boards stacked flat on Z give no dowel; a hole larger than the face, including `Dia + 2 * Cl`, is dropped; two separate stacks with `N = 1` return one line; two pieces whose thicknesses point different ways do not share a dowel; a stud touched on the narrow edge gets none, and the same stud touched on the 3.5 × 20 face gets one dowel through the 1.5 thickness; four pieces on one footprint are one line of score 4; a pair that overhangs that footprint is a second, lower-ranked line; an X dowel and a Y dowel closer than one hole-diameter keep only the higher line. MSBuild Debug succeeded (only the pre-existing `CS0472` warnings). `.gha` wrote to `bin/Debug/net48/SpruceBeetle.gha`. The Grasshopper viewport was not opened.
- **Follow-ups:** Reload Grasshopper. Wire Bin Packing `Oc` into Dowel Column on a Longest Z column and confirm the cylinders are horizontal through the wide faces. Component-Reference is updated. Cutting the holes into the offcuts is a later pass.

### 2026-10-05 — fix: locked packing no longer bridges holes

- **Motivation:** Longest Z / Shortest Z kept a single vertical side, but EB-AFIT still packed in flat courses. A board could sit across a shorter neighbor and leave a shelf of air underneath. The ragged top of each stack is a separate issue and is left as-is.
- **Files:** `Packing/AxisLockPacker.cs`, `Packing/BinPackingCS_GH.cs`, `Packing/EB_AFIT_AxisLock.cs` (removed), `Documentation/Component-Reference.md`, `Documentation/Column-Fill-2x2x8.md`
- **Before → after:** Locked modes called the filtered EB-AFIT copy. They now place each piece at the lowest plan position where its whole bottom touches the floor or the tops of pieces already there. A piece that would have to bridge a hole is omitted. `Unlimited` still uses the NuGet packer. On a 24×24×96 box, shelf air under packed boards (not the open top) went to 0: `stock_short_scraps.csv` Longest Z stayed at 100 pieces and the column height dropped from 39″ to 20″; `stock_column_in.csv` Longest Z went from 21 pieces to 26; `stock_2x4_scraps.csv` Longest Z was already solid (100 pieces) and stayed there. Shortest Z on mixed sections omits more pieces instead of bridging (`stock_short_scraps.csv` 100→88, `stock_column_in.csv` 33→16).
- **Result / observation:** The shelves were a layer-packer artifact. Refusing a placement unless the footprint is fully supported removes them. Uneven stack tops remain, because a short board beside a long one is open above.
- **Follow-ups:** Close Rhino and rebuild. This Debug compile succeeded, but `bin/Debug/net48/SpruceBeetle.gha` was locked by Rhino 8, so the running plugin does not have the tighter placement yet. Then re-pack the column in Longest Z. Component-Reference is updated.

### 2026-10-02 — feature: Dowel Column places pieces on a chosen number of vertical dowels

- **Motivation:** The column box should be a boundary. A chosen number of vertical dowels is what each piece has to hit, so placement can follow a dowel line instead of filling the box with EB-AFIT.
- **Files:** `Packing/DowelColumn.cs`, `Packing/DowelColumn_GH.cs`, `Documentation/Component-Reference.md`
- **Before → after:** Packing only filled a box with EB-AFIT and never looked at a line. New component **Dowel Column** (`DowelCol`, GUID `C4A91E72-6B38-4F05-9D14-2E7B8A0C5F61`, PackBin icon). Inputs: offcuts, box, dowel count `N` (default 3), optional points, lap `L` (default 1). `N = 1` puts one dowel at the section center. `N ≥ 2` puts dowels on a regular polygon at 75% of the inscribed radius, first vertex toward −Y. Points inside the section replace that layout; `N` above 32 is capped. Each piece must contain `min(2, N)` dowels. Solids stay inside the supplied box. The six axis assignments are tried; the larger footprint wins when several poses sit at the same height. Pieces that share a dowel and overlap in plan overlap in Z by the lap (that band is shared volume). Pieces that only meet on a face may sit at the same height. A piece that cannot cover the dowels is unused.
- **Result / observation:** Checked the placer outside Grasshopper. In a 24×24×96 column with lap 1: three 10×10×20 blocks on one center dowel all placed, lying 10″ tall at Z = 0, 9, 18. Two 10×10×50 blocks on that dowel: one placed, because 50+50−1 = 99″ exceeds 96″. Three 16×3.5×8 studs on the 3-dowel polygon placed and lapped by 1″; the second stud reached the third dowel; a 2×2×8 did not span a pair. A 30×10×10 on one dowel stood with the 30″ side on Z, because 30″ does not fit in the 24″ plan. MSBuild Debug succeeded (only the pre-existing `CS0472` warnings). `.gha` wrote to `bin/Debug/net48/SpruceBeetle.gha`. The Grasshopper viewport was not opened.
- **Follow-ups:** Reload Grasshopper and run `stock_2x4_scraps.csv` with `N = 1` and `N = 3` in a 24×24×96 box. Component-Reference is updated.

### 2026-10-02 — feature: orientation lock on Bin Packing

- **Motivation:** Column fills were mixing upright sticks and flat boards in one pack. Both layouts are useful; they should not share a column unless the user asks for free rotation.
- **Files:** `Packing/BinPackingCS_GH.cs`, `Packing/EB_AFIT_AxisLock.cs`, `Documentation/Component-Reference.md`, `Documentation/Column-Fill-2x2x8.md`
- **Before → after:** PackBin had no rotation control. EB-AFIT tried all 6 item orientations and all 6 container orientations. New input `Or`: `Unlimited` (default, same NuGet packer), `Longest Z` (longest side on world Z), `Shortest Z` (thickness on world Z). The other two sides may still swap in plan. Pieces that do not fit the locked pose are omitted. On a 24×24×96 column a stud longer than 24″ cannot lie flat, so `Shortest Z` skips it.
- **Result / observation:** The NuGet class has no rotation flag and its loops are private, so the locked modes are a local MIT copy of EB-AFIT (`EB_AFIT_AxisLock`) limited to container variants 1–2. `Unlimited` still calls `CromulentBisgetti.ContainerPacking` 1.0.0. A checked pack of 1.5×3.5×16, 1.5×3.5×48, and 10×10×4 into 24×24×96 put the locked side on library height (world Z) for both locks, and left the 48″ stick unpacked under `Shortest Z`.
- **Follow-ups:** Close Rhino and rebuild. The Debug compile succeeded, but `bin/Debug/net48/SpruceBeetle.gha` was locked by Rhino 8 so the running plugin does not have `Or` yet. Then pack a column with each value and confirm the viewport. Component-Reference is updated.

### 2026-10-01 — experiment: drop scraps shorter than 8 inches

- **Motivation:** Pieces under 8″ are hard to hold on the table, so the scrap piles should only include stock that can be fixtured.
- **Files:** `Documentation/TestData/stock_2x4_scraps.csv`, `Documentation/TestData/stock_short_scraps.csv`
- **Before → after:** `stock_2x4_scraps.csv` started at 4″ (nine pieces from 4″ to 7.5″). Those lengths are now 8″, 10″, 12″, 14.5″, 16″, 18″, 24″, 36″, and 48″; the file is still 100 dressed `1.5 × 3.5` sticks, shortest 8″, longest 92″. `stock_short_scraps.csv` started at 2″. Eighteen studs under 8″ were replaced with lengths from 8″ to 19.5″, and seven non-stud lengths under 8″ were raised into that same band (sections unchanged). Both files still have 100 rows. Short-file lengths stay under 20″.
- **Result / observation:** Minimum length in both scrap files is 8″. An 8″ piece is kept; only lengths below 8″ were removed.
- **Follow-ups:** Reload the CSV in Grasshopper before the next pack. Component-Reference does not need an update (test data only).

### 2026-09-30 — experiment: short scrap pile under 20 inches

- **Motivation:** Column-fill tests also need a bin of short offcuts, mostly stud stock, with a minority of other thin sections, instead of only the longer 2×4 leftovers.
- **Files:** `Documentation/TestData/stock_short_scraps.csv`
- **Before → after:** No short mixed-section scrap file → 100 rows, `index;x;y;z` in inches. Rows 1–75 are dressed studs `1.5 × 3.5`, lengths 2″–19.5″ (shortest first), including repeated 6″, 8″, 12″, 14.5″, 16″, and 18″ cuts. Rows 76–100 are other sections with thickness 0.75″–2″ (1× boards, 2×2 through 2×12, a few 1″ and 1.75″, and three rough pieces at 2″ thick). Every length is under 20″.
- **Result / observation:** Stud rows stay the dressed 1½″ × 3½″ section. The other rows keep thickness in `x` at 2″ or below and put the wider face in `y`. Nothing in this file is a full stud or a piece 20″ or longer.
- **Follow-ups:** Pack `stock_short_scraps.csv` into the 24×24×96 column. Component-Reference does not need an update (test data only).

### 2026-09-30 — experiment: 100 dressed 2×4 stud scraps as packing stock

- **Motivation:** Column-fill tests need a scrap pile whose cross-section matches real stud stock, so packing uses the lumber you can actually buy rather than nominal names or the earlier mixed-section files.
- **Files:** `Documentation/TestData/stock_2x4_scraps.csv`
- **Before → after:** No 2×4-specific stock file → 100 rows, `index;x;y;z` in inches, every piece `1.5 × 3.5` (dressed size) with lengths from 4″ to 92″ (leftovers from a 96″ stud, including repeated 14.5″ and 22.5″ blocking). Sorted shortest to longest. No full 96″ sticks.
- **Result / observation:** Nominal 2×4 is 2″ × 4″; surfaced dry studs are 1½″ × 3½″, and an 8 ft stud is 96″ long (length is not dressed down). The CSV uses the dressed section so the box matches physical stock. All pieces fit a 24″ × 24″ × 96″ column on the length axis.
- **Follow-ups:** Pack `stock_2x4_scraps.csv` into the 24×24×96 column. Component-Reference does not need an update (test data only).

### 2026-09-25 — feature: Clearance slider on every joint cutter

- **Motivation:** Loose keys were cut to the same size as the pocket, so a key had no fitting gap. Shop clearance only needs a few thousandths of an inch.
- **Files:** `Base/ClearanceSlider.cs`; `Base/Joint.cs`; `Packing/ContactTenon_GH.cs`; `Packing/ContactSpline_GH.cs`; `Packing/OutsideKey_GH.cs`; `Alignment/TenonJoints_GH.cs`; `Alignment/SplineJoints_GH.cs`; `Alignment/IntersectionJoints_GH.cs`; `Documentation/Component-Reference.md`; `Documentation/Packing-Joints.md`; this log.
- **Before → after:** Joint components had no clearance pin. Each now has `Cl` at the end of its inputs (existing wires stay put). If `Cl` is unwired, a number slider is added with init code `0.001<0.005<0.01` (default `0.005`). `J` stays the key size. The boolean cutter is larger by `Cl` on each closed side. Centered tenons and contact splines also add `Cl` at each end of the depth. Outside keys add `Cl` inward only. Intersection laps offset the cutter solid by `Cl`.
- **Result / observation:** Compiles (MSBuild Debug; only pre-existing `CS0472`). Copy to `bin/` blocked — Rhino 8 (PID 37704) holds the `.gha`. Built assembly is in `obj/Debug/net48/`. Viewport check still needs a Rhino reload. A `0.005` slider makes a `1` key cut a `1.01` pocket.
- **Follow-ups:** Close Rhino and reload. Confirm the slider range is `0.001` to `0.01` and that `J` is smaller than the notch.

### 2026-09-25 — feature: Outside Key cuts face keys on the packed column skin

- **Motivation:** Contact Spline opens a channel along the mating face and takes the first clear edge, usually world +Z. The shop lock is a pocket on the outer vertical face, overlapping the seam, inserted after the stack is up.
- **Files:** `Packing/PackedNeighbors.cs`; `Packing/OutsideKey_GH.cs`; `Base/Joint.cs`; `Documentation/Packing-Joints.md`; `Documentation/Component-Reference.md`; `Documentation/README.md`; `Documentation/Column-Fill-2x2x8.md`; this log.
- **Before → after:** Packing cutters were Contact Tenon (blind) and Contact Spline (edge-open along the contact). New **Outside Key** (`OutsideKey`, GUID `9B4E7D12-…`) takes packed `Oc` + contacts and cuts a rectangular (or custom) pocket into ±X/±Y of the packed AABB where both members share that face. `JT` value list is `rectangular` / `custom key`. `J` is flush with the face; `Dir` points inward. Stepped and interior seams skip. Contact Tenon and Contact Spline are unchanged.
- **Result / observation:** Compiles (MSBuild Debug; only pre-existing `CS0472`). Copy to `bin/` blocked — Rhino 8 (PID 30752) holds the `.gha`. Built assembly is in `obj/Debug/net48/`. Viewport proof (keys on flush cheeks, skip on 43/58 step, skip on lid) still needs a Rhino reload.
- **Follow-ups:** Close Rhino and reload. Wire Packed Contacts (All, or Z plus XY) into Outside Key. Confirm no keys on the top face or on stepped seams.

### 2026-09-23 — fix: Contact Spline key fills the slot

- **Motivation:** Edge-open keys were coming out `JX` long (1.25 in the column test) while the cut runs from the open edge to the closed stop. The viewport showed a short solid and a drive line for the rest of the joint.
- **Files:** `Packing/ContactSpline_GH.cs`; `Packing/PackedNeighbors.cs`; `Documentation/Packing-Joints.md`; `Documentation/Component-Reference.md`; this log.
- **Before → after:** `J` was a `JX` box against the closed stop. Now `J` spans the slot: open edge to the stop (`run − D`). The cutter still overruns the mouth by `0.01`; the key stops flush with the edge. `JX` stays on the component so `JY` / `Dep` / `R` / `TC` wires do not shift, and the value is ignored. The mouth probe is the key length, not `JX`, so a mouth blocked for the full key is skipped.
- **Result / observation:** Compiles (MSBuild Debug; only pre-existing `CS0472`). `.gha` wrote to `bin/Debug/net48/`. A contact that fit a 1.25 key may now skip if there is not a clear path as long as the slot outside the mouth. Viewport check still needs a Rhino reload.
- **Follow-ups:** Close Rhino and reload. Confirm `J` matches the cut from mouth to stop. If dense XY stitches skip, the probe length is the reason.

### 2026-09-23 — fix: Contact planes stay in the viewport and are not baked

- **Motivation:** Packed Contacts, Select Contacts, Contact Tenon, and Contact Spline draw contact planes that are useful in the viewport. Baking the component was also baking those planes.
- **Files:** `Packing/PackedContacts_GH.cs`; `Packing/SelectContacts_GH.cs`; `Packing/ContactTenon_GH.cs`; `Packing/ContactSpline_GH.cs`; `Documentation/Packing-Joints.md`; `Documentation/Component-Reference.md`; this log.
- **Before → after:** Component Bake wrote every bakeable output, including plane parameters (`P` and `Sk`). Bake now skips `Param_Plane`. Preview is unchanged. Overlap rectangles (`R` on Packed Contacts) still bake.
- **Result / observation:** Compiles with the key-length change (MSBuild Debug; only pre-existing `CS0472`). `.gha` wrote to `bin/Debug/net48/`. Bake behavior still needs a Rhino check.
- **Follow-ups:** Bake a Contact Spline and a Packed Contacts component. Planes should stay out of the document; joints, offcuts, and overlap rectangles should still bake.

### 2026-09-23 — fix: Contact Spline mouth is square to the edge

- **Motivation:** Edge-open spline has to accept a rectangular key. Filleting every cutter corner rounded the mouth where it breaks the member edge.
- **Files:** `Packing/ContactSpline_GH.cs`; `Documentation/Packing-Joints.md`; `Documentation/Component-Reference.md`; this log.
- **Before → after:** `CreateFilletCornersCurve` on the full slot rectangle → profile fillets only the two closed-stop corners; the mouth stays 90 degrees. `R` still mill-rounds the inside stop.
- **Result / observation:** Compiles (MSBuild Debug; only pre-existing `CS0472`). Copy to `bin/` blocked — Rhino 8 (PID 2088) holds the `.gha`. Built assembly is in `obj/Debug/net48/`. Viewport check after reload: the opening on the face should be a rectangular notch, not a bullnose.
- **Follow-ups:** Close Rhino and reload. Check a cut XY stitch against the previous curved mouth.

### 2026-09-23 — feature: Contact Spline cuts edge-open keys after the column is stacked

- **Motivation:** Two perpendicular captured tenons on one stick cannot be closed in one move (each joint needs the other to be absent or already finished). An edge-open spline lets both scraps sit in their packed pose, then a loose key slides in through a free edge.
- **Files:** `Packing/PackedNeighbors.cs`; `Packing/ContactSpline_GH.cs`; `Documentation/Packing-Joints.md`; `Documentation/Component-Reference.md`; `Documentation/README.md`; this log.
- **Before → after:** Packing cutters were Contact Tenon only (blind pocket, inset `D` on all sides). New **Contact Spline** (`ContactSpline`, GUID `A8D31C47-…`) takes the same `Oc` + `PackedContact` list and cuts a rectangular channel that breaks one unblocked overlap edge. Mouth order is world `+Z` on vertical contacts, then long side, then short side. `J` is the `JX` key against the closed stop; `Dir` is mouth toward stop. Contact Tenon is unchanged.
- **Result / observation:** Compiles (MSBuild Debug; only pre-existing `CS0472`). `.gha` wrote to `bin/Debug/net48/` this build. Viewport proof (open mouth, `Dir` arrow, skip on a buried bed) still needs a Rhino reload. Typical split: Tenon on `Z`, Spline on `XY`. Buried mouths (Z bed boxed in by a course) skip; those contacts stay on Contact Tenon.
- **Follow-ups:** Close Rhino and reload. Preview `Dir` on an XY stitch. Confirm a buried bed goes to `SkC`. Component-Reference packing section includes Contact Spline.

### 2026-09-19 — feature: Contact Tenon skips undersized faces (D edge meat) and outputs SkC

- **Motivation:** Column screenshots showed a pocket opening to a vertical edge on a thin bed. Centering was correct; `JX`/`JY` were allowed to fill the overlap flush, so the boolean chewed the rim. Need a mill inset, a way to see skips, and a way to retry those contacts with a smaller tenon.
- **Files:** `Packing/PackedNeighbors.cs`; `Packing/ContactTenon_GH.cs`; `Documentation/Packing-Joints.md`; `Documentation/Component-Reference.md`; this log.
- **Before → after:** Skip test was `JX×TC ≤ overlap long` and `JY ≤ overlap short` (flush allowed). Now the overlap is inset by **`D` on all sides** (`usable = overlap − 2D`). New output **Skipped Contacts `SkC`** (same GUID; `Sk` planes unchanged at index 3). `SkC` is the skipped `PackedContact` list so a second Contact Tenon can cut them with a smaller `JX`/`JY`. Second pass must take the first pass `Oc` (already cut), not the original pack list.
- **Result / observation:** Compiles (MSBuild Debug; only pre-existing `CS0472`). Copy to `bin/` blocked — Rhino 8 (PID 28736) holds the `.gha`. A `D = 0.375`, `JX = 1.338` tenon now needs an overlap of at least `1.338 + 0.75` on the long side; the thin shoulder in the screenshot should skip.
- **Follow-ups:** Close Rhino and rebuild. Preview `Sk` to see skip planes. Wire `SkC` → second Contact Tenon `C` if those faces should still get a smaller joint.

### 2026-09-19 — feature: Alignment Tenon Joints uses Tool Diameter like Contact Tenon

- **Motivation:** Contact Tenon is driven by bit diameter. Alignment Tenon still used `R` as the mill input, and the "Nice Size Tenon" slider labeled Tool Radius was already set to `0.25` (a ¼″ bit diameter, not a fillet). Same mill language on both cutters.
- **Files:** `Alignment/TenonJoints_GH.cs`; `Documentation/Component-Reference.md`; `Documentation/Packing-Joints.md`; this log.
- **Before → after:** Pin 1 **Tool Radius `R`** (default `0.005`, used as fillet) → **Tool Diameter `D`** (default `0.01` = twice the old radius, so unwired fillet stays `0.005`). Existing wires on pin 1 keep their number and now mean diameter; fillet is `D / 2`. `JX` / `JY` raised to `D` (warns). New optional `R` after `CS` for a larger fillet; unwired or smaller than `D / 2` uses `D / 2` with no warning. `JZ`, `JT`, `TC`, `CS` indices unchanged. GUID unchanged. `ApplyPinNames` retitles pin 1 on old canvases.
- **Result / observation:** A slider of `0.25` on the old R pin becomes a ¼″ bit with `0.125` corners instead of a 0.25″ fillet. Spline Joints still uses `R` as fillet (not changed).
- **Follow-ups:** Close Rhino and rebuild. Relabel the Nice Size Tenon slider from Tool Radius to Tool Diameter. Optional: same `D` treatment on Spline Joints.

### 2026-09-19 — fix: Contact Tenon does not warn when raising R to D/2

- **Motivation:** Unwired `R` defaults to `0.125`. With `D = 0.375` the component correctly used `0.1875` but painted the box orange, which read as a failed solve even though 63 contacts cut.
- **Files:** `Packing/ContactTenon_GH.cs`; `Documentation/Packing-Joints.md`; `Documentation/Component-Reference.md`; this log.
- **Before → after:** Floor `R` to `D / 2` still happens; the runtime warning is gone. `JX` / `JY` raised to `D` still warn.
- **Result / observation:** Compiles (MSBuild Debug; only pre-existing `CS0472`). Copy to `bin/` blocked — Rhino 8 (PID 25928) holds the `.gha`. Built assembly is in `obj/Debug/net48/`.
- **Follow-ups:** Close Rhino and rebuild. Unwired `R` with `D = 0.375` should stay grey except for the cut-count remark.

### 2026-09-19 — feature: Contact Tenon takes Tool Diameter as a mill constraint

- **Motivation:** Shops talk in bit diameter, and the earlier `W` pin ("Width Factor") read like a tenon width but was actually a multiplier (`pocket = W × D`). Confirmed this session that the confusion was the pin name, not the geometry. Wanted the bit size back as an explicit constraint without it silently setting the joint size again.
- **Files:** `Packing/ContactTenon_GH.cs`; `Documentation/Packing-Joints.md`; `Documentation/Component-Reference.md`; this log.
- **Before → after:** Inputs were `Oc`, `C`, `JX`, `JY`, `Dep`, `R`, `JT`, `TC`, `CS`. Added **Tool Diameter `D`** (default `0.25`) at index 2, so `JX` shifts to 3 and `CS` to 9. `D` is a **floor only**: `JX` and `JY` are raised to `D`, `R` is raised to `D / 2` (the tightest inside corner the bit can cut), each with a runtime warning naming the new value. `R` is still capped below `min(JX, JY) / 2`, which wins over the floor when a tenon is exactly `D` wide. `Dep` is unchanged (free value, still clamped to `thinner / 3` so a tenon cannot punch through). GUID unchanged.
- **Result / observation:** Compiles (MSBuild Debug; only the pre-existing `CS0472` pair). Copy to `bin/` blocked — Rhino 8 (PID 20036) holds the `.gha`; the built assembly is in `obj/Debug/net48/`. Rejected the literal reading "radius of the tool diameter or larger" (floor `D`) in favor of `D / 2`, since a `D` bit physically cuts a `D / 2` corner; user confirmed. Also rejected collapsing `JX` / `JY` into one square Width — user wants long narrow keys as well as square pegs.
- **Follow-ups:** Close Rhino, rebuild so the copy lands in `bin/`, then re-wire Contact Tenon (pins shifted by one from `JX` onward) and run the column test.

### 2026-09-19 — docs: packing nicknames, icons, and catalog sync

- **Motivation:** Packing tab names and the user catalog still described the adapter path (Packed Stacks, Contact Joints / `PackJoints`, `PackBinC#`) after Contact Tenon shipped.
- **Files:** `Packing/BinPackingCS_GH.cs`; `Packing/SelectContacts_GH.cs`; `Packing/PackedContacts_GH.cs`; `Documentation/Packing-Joints.md`; `Documentation/Component-Reference.md`; `Documentation/Column-Fill-2x2x8.md`; `Documentation/README.md`; this log.
- **Before → after:** Nickname `PackBinC#` → **PackBin**; `PickJoints` → **PickContacts** (`ApplyDisplayNames` retitles existing boxes). Packed Contacts icon Intersection Joints → Find Intersections. Contact Tenon icon Tenon Joints. Component-Reference packing section now lists `Oc`, Packed Contacts, Select Contacts, and Contact Tenon. Column-Fill points joints at that path. Select Contacts still uses the Unification icon (cosmetic debt).
- **Result / observation:** GUIDs unchanged. Label Offcut Numbers and Used Offcuts stay on Create.
- **Follow-ups:** Close Rhino so existing boxes pick up the new nicknames.

### 2026-09-19 — feature: Contact Tenon full parity with Alignment Tenon knobs

- **Motivation:** Packed-column joints need matching pockets on both members at real face contacts, with the same size / type / count controls as Alignment Tenon, without feeding a contact graph into a chain cutter.
- **Files:** `Packing/ContactTenon_GH.cs` (was `Packing/ContactJoints_GH.cs`); `Packing/PackedNeighbors.cs`; `Documentation/Packing-Joints.md`; `Documentation/Component-Reference.md`; this log.
- **Before → after:** Contact Joints (`PackJoints`) cut one square `W×D` pocket per contact using `CenterOnOverlap`. **Contact Tenon** (`ContactTenon`, GUID `7C2F5B18-…` unchanged) takes `JX`/`JY`/`Dep`/`R`/`JT`/`TC`/`CS`, types tenon / cross / custom (auto value list), and cuts the same solid from both members. `OrientOnOverlap` places the frame at the overlap-rectangle center with X along the longer in-plane side. Skip if `JX×TC` exceeds the long side or `JY` exceeds the short side. Depth is `min(Dep, thinner/3)`, centered. `J` is the tenon body; `JV` is per-solid volume. `D`/`W` pins are gone — re-wire old canvases. `ApplyDisplayNames` retitles Contact Joints boxes.
- **Result / observation:** Compiles (MSBuild Debug; only pre-existing `CS0472` in DeconstructOffcut). `.gha` wrote to `bin/Debug/net48/`. Not yet re-tested in Grasshopper; column cut/skipped counts still unknown for the new knobs (defaults `JX=JY=1`, `Dep=0.5`).
- **Follow-ups:** Close Rhino, reload. Re-run `stock_column_in.csv` pack → Packed Contacts → Select Contacts → Contact Tenon and record cut/skipped counts in Packing-Joints.md.

### 2026-09-19 — experiment: delete Packed Stacks (adapter into Alignment Tenon failed)

- **Motivation:** Packed Stacks existed only to graft Z-groups into Alignment Tenon. Contact Tenon now cuts Z beds and XY stitches from Packed Contacts, so the adapter has no remaining joint use case. Keeping it would keep a known-wrong path on the ribbon.
- **Files:** deleted `Packing/PackedStacks_GH.cs`; `Packing/PackedNeighbors.cs` (`ZStacks` / `ZContact` removed); `Packing/BinPackingCS_GH.cs` (`Oc` description); `Documentation/Packing-Joints.md`; this log.
- **Before → after:** Spec said keep Packed Stacks as a parallel Z-only path. Deleted. Union-find stacks were not linear chains (a bridging piece grouped side-by-side neighbors), and Tenon centered each cut on that piece's own `FirstPlane`/`SecondPlane` (packing sets those at the piece bbox center, not the shared overlap). Result on packed offcuts: mating pockets offset, overlapping tenons, and joints where nothing touched. Canvases that still contain Packed Stacks (GUID `8F3A6C21-…`) show a missing-component placeholder.
- **Result / observation:** The 2026-09-02 / 2026-09-08 “keep both paths” decision is reversed. Alignment Tenon is unchanged and remains the curve-chain cutter.
- **Follow-ups:** Remove leftover Packed Stacks boxes from `Initial_Tests.gh` when that file is next opened.


### 2026-09-19 — fix: drop Select Contacts Seams and Connected

- **Motivation:** T-junction placement is gone (overlap-center pockets). Seams was the leftover filter for those junctions and already kept 88 of 88 on the column test. Connected only existed to prefer those “seams” then stop at a spanning set — no separate thesis use once All / Z / XY remain.
- **Files:** `Packing/SelectContacts_GH.cs`; `Packing/PackedNeighbors.cs`; `Documentation/Packing-Joints.md`; this log.
- **Before → after:** Modes All / Z / XY / Seams / Connected → **All / Z / XY**. Deleted `SharesSeam`, `SelectSeams`, `SelectConnected`, `Rank`, `BoxesOverlap`. Pins `N` (piece count) and `T` (seam gap) removed; inputs are `C` then `M`. GUID unchanged. Auto value list is rewritten to the three modes on solve. A leftover `Seams` or `Connected` string warns and behaves as All.
- **Result / observation:** Not yet re-tested in Grasshopper. Existing PickJoints boxes may need `M` re-wired if Grasshopper shifted the Mode pin from index 2 to 1.
- **Follow-ups:** Close Rhino and rebuild. Confirm the value list shows All / Z / XY. Use All for every pocket, Z for beds, XY for side stitches.

### 2026-09-19 — feature: Contact Joints pockets centered on the shared overlap rectangle; T-junction placement removed

- **Motivation:** Contact Joints (packed `Oc` → Packed Contacts → Select Contacts → cut) skipped 65 of 88 contacts on the column test because the pocket had to sit inset `D` from a detected third-piece edge. Decision this session: place each pocket at the **center of the smallest shared contact surface** — the overlap rectangle where the two touching faces intersect — and drop the T-junction / seam logic as too complicated for the thesis scope.
- **Files:** `Packing/PackedNeighbors.cs`; `Packing/ContactJoints_GH.cs`; `Documentation/Packing-Joints.md`; this log.
- **Before → after:** `PackedNeighbors.OffsetTowardSeam` (plus private `EdgeSeams`, `TryPlaceOnEdge`, `TouchesX`, `TouchesY`, ~130 lines) deleted. New `CenterOnOverlap(contact, width, out plane, out canCut)` returns the overlap-rectangle center plane along the contact axis and `canCut = overlap ≥ pocket width in both in-plane directions`. Contact Joints calls it instead; `D` no longer acts as an inset, only width / depth cap / fillet. Component, `D`, and `Sk` descriptions updated. GUID, inputs, and outputs unchanged. The center plane equals Packed Contacts' `P` output for that contact. Select Contacts **Seams** mode and Connected's seam-first ranking are untouched (still the known 88/88 bug).
- **Result / observation:** Compiles (MSBuild, only pre-existing warnings); copy to `bin/` blocked by Rhino 8 holding the `.gha`. Not yet re-tested in Grasshopper.
- **Follow-ups:** Close Rhino, rebuild, re-run the column test and record cut/skipped counts in Packing-Joints.md (expect far fewer skips; remaining skips = overlap narrower than `W × D` or boolean fail). Decide whether Select Contacts Seams / Connected should also lose the T-junction logic. Component-Reference still does not document Contact Joints.

### 2026-09-19 — fix: Used Offcuts title sticks on existing canvases

- **Motivation:** Shop pick list was still reading as **Used Indices** / `UsedI` on placed components. “Index” is the Offcut field name, not how scraps are talked about.
- **Files:** `Create/UsedOffcuts_GH.cs` (was `Create/UsedIndices_GH.cs`); `Documentation/Component-Reference.md`; this log.
- **Before → after:** Constructor already said Used Offcuts, but Grasshopper restores the serialized name from the `.gh` file. After Read / AddedToDocument the title is forced to **Used Offcuts** (`UsedOc`). Class/file renamed; GUID unchanged. Pins `U`/`Un` still output scrap numbers. Search “Used Indices” will not find a second component.
- **Result / observation:** N/A until Rhino is restarted with the new `.gha`.
- **Follow-ups:** Close Rhino and rebuild. Existing Used Indices boxes should retitle without re-wiring.

### 2026-09-14 — fix: Label Offcut Numbers prefers exposed (non-contact) faces

- **Motivation:** Packed-column numbers were only readable on +X/+Y. Equal-area opposite faces always tied to the larger X then Y, so the −X/−Y half of the stack was engraved on inner contact faces.
- **Files:** `Create/LabelOffcutNumbers_GH.cs`; `Packing/PackedNeighbors.cs`; `Documentation/Component-Reference.md`; `Documentation/Column-Fill-2x2x8.md`; this log.
- **Before → after:** Largest vertical face, +X/+Y tie-break → prefer an uncovered vertical patch (AABB neighbor overlap, gap `0.01"`), else an exposed top, else any vertical face if the piece is fully buried. Partial covers place the number on the largest empty rectangle of that face. Failed faces fall through to the next candidate.
- **Result / observation:** Explains the “labels only on positive X,Y” screenshot: not missing Index values, hidden interior cuts. GUID unchanged.
- **Follow-ups:** Close Rhino and rebuild. Orbit the column: −X/−Y outer faces should show numbers; the component remark reports exposed vs buried counts. Interior pieces may still be labeled on a contact face.

### 2026-09-14 — fix: Label Offcut Numbers cuts every digit of a two-digit Index

- **Motivation:** Column labels must match the unique scrap / CSV number. Repeated 1s and 4s on different pieces made the pack unreadable.
- **Files:** `Create/LabelOffcutNumbers_GH.cs`; `Documentation/Component-Reference.md`; `Documentation/Column-Fill-2x2x8.md`; this log.
- **Before → after:** `CreateTextOutlines` makes one solid per glyph. Join/Union of disjoint digits failed, so the cutter fell back to `parts[0]` (the first digit only). 11 and 19 both showed as 1; 40–49 as 4. Now every glyph is subtracted (batch boolean, then per-digit fallback). Partial cuts are rejected so a lone first digit is not kept.
- **Result / observation:** Used Offcuts was already unique (5, 9, 11, 19…). The viewport numbers were truncated Index values, not duplicate stock. LabelN input from packing `Oc` is correct; its `Oc` was unwired from Get Brep / Contact Joints (preview-only).
- **Follow-ups:** Close Rhino and rebuild. Confirm packed pieces show full numbers (11, 19, 33, 48…), not single digits. Rhino MCP was not connected this session.

### 2026-09-14 — feature: Label Offcut Numbers engraves Index on the largest vertical face

- **Motivation:** Shop/model labels should be 3D letters cut into an outside face (like `_TextObject` curves + gumball Cut), not center dots. One largest vertical face per piece.
- **Files:** `Create/LabelOffcutNumbers_GH.cs`; `Documentation/Component-Reference.md`; `Documentation/Column-Fill-2x2x8.md`; this log.
- **Before → after:** `LabelN` placed Rhino `_Dot`s at AveragePlane → cuts Index into the largest vertical Brep face (`Curve.CreateTextOutlines` + inward BooleanDifference). GUID unchanged. Inputs `S` (letter height, default 1, clamped to face) and `Dep` (default 0.125, max ~40% of thickness). Outputs `Oc` (cut Offcuts), `C` (letter solids), `Sk` (failed faces). Old `D` dot pin is gone (re-wire).
- **Result / observation:** N/A until a column pack is re-tested in Grasshopper. Boolean fail keeps the uncut solid.
- **Follow-ups:** Close Rhino and rebuild. Check a packed `Oc` list: numbers upright on the big side, holes in 8/0/6/9, no punch-through on thin scraps.

### 2026-09-14 — feature: Label Offcut Numbers places Rhino dots on packed pieces

- **Motivation:** After packing, need to see which numbered scrap sits where in the column (Used Offcuts is only a sorted pick list).
- **Files:** `Create/LabelOffcutNumbers_GH.cs`; `Documentation/Component-Reference.md`; `Documentation/Column-Fill-2x2x8.md`; this log.
- **Before → after:** No in-model labels → Create component **Label Offcut Numbers** (`LabelN`): packed/aligned `Oc` → text dots at AveragePlane (Brep bbox center fallback), text = Index, optional height `S` default 2. Not sorted. Bake `D` to keep dots in the `.3dm`.
- **Result / observation:** Grasshopper 7.15 has no `AddTextDotParameter`; dots go out as generic goo plus component preview/bake. Dots sit at piece centers and can hide inside a dense pack; ghost geometry or hide `POc` to read them. Offset/explode left for later.
- **Follow-ups:** Rebuild after closing Rhino; confirm dots match CSV numbers on a column pack.

### 2026-09-14 — feature: Sort Used Offcuts lists numerically

- **Motivation:** Shop pick list should match numbered scraps in a pile and a CSV, not packing/build order.
- **Files:** `Create/UsedIndices_GH.cs`; `Documentation/Component-Reference.md`; this log.
- **Before → after:** `U` followed packed/aligned placement order (e.g. 88, 42, 82…) → `U` and `Un` sorted by Index (1, 2, 3…).
- **Result / observation:** Placement order is still on packing `Oc` if a walk-the-column list is needed later.
- **Follow-ups:** Close Rhino and rebuild so the sorted list loads.

### 2026-09-14 — feature: Rename Used Indices to Used Offcuts

- **Motivation:** Shop language is which offcuts were used, not “indices.”
- **Files:** `Create/UsedIndices_GH.cs`; `Documentation/Component-Reference.md`; `Documentation/Column-Fill-2x2x8.md`; `Documentation/Packing-Joints.md`; this log.
- **Before → after:** Display name / nickname **Used Indices** (`UsedI`) → **Used Offcuts** (`UsedOc`). GUID unchanged; still outputs used/unused stock numbers, not Offcut objects.
- **Result / observation:** N/A — rename only.
- **Follow-ups:** Rebuild and restart Grasshopper so the canvas shows the new name.

### 2026-09-14 — feature: Used Indices lists written scrap numbers after packing or alignment

- **Motivation:** Shop workflow: number scraps as they are measured (CSV column 1), then after Bin Packing or Curve Alignment know which labeled pieces went into the column / curve.
- **Files:** `Create/UsedIndices_GH.cs`; `Documentation/Component-Reference.md`; `Documentation/Column-Fill-2x2x8.md`; `Documentation/Packing-Joints.md`; this log.
- **Before → after:** Used scrap IDs were only on Deconstruct Offcut pin `i` (buried among planes); packing docs said IDs were dropped. New Create component **Used Indices** (`UsedI`): packed/aligned `Oc` → used numbers in placement order; optional full stock `OcD` → leftover numbers in CSV order. Packing/alignment logic unchanged (Index was already preserved).
- **Result / observation:** Index is the number written on the wood. Alignment already had leftover Offcut objects (`UOc`); packing still has no unused Offcut list — unused **numbers** come from `OcD` minus packed `Oc`.
- **Follow-ups:** Place `UsedI` on a column-fill canvas and confirm Panel `U`/`Un` match CSV labels (including skipped numbers such as missing 11). Optional later: 3D tags on packed solids.

### 2026-09-12 — setup: Restore Custom Joints; do not delete components

- **Motivation:** Cleanup had removed `Alignment/CustomJoints.cs` as dead code (entire class commented out). User rule: do not delete any components.
- **Files:** `Alignment/CustomJoints.cs` restored from git; `Setup/Repo-Cleanup-Todo.md`; this log.
- **Before → after:** Custom Joints file deleted on disk → same commented-out source as before cleanup. Still not registered in Grasshopper (class remains commented).
- **Result / observation:** Hygiene can drop unused icons and junk files. Component `.cs` files stay, even if commented out.
- **Follow-ups:** Leave Custom Joints archived in-place unless it is uncommented on purpose.

### 2026-09-12 — setup: Repo hygiene, docs index, and fork identity

- **Motivation:** Build artifacts and leftover files were in git; README still described Dominik’s upstream repo. Needed a cleaner working copy without changing packing or joint C#.
- **Files:** `.gitignore`; untracked `bin/` `obj/` `.DS_Store`; `SpruceBeetle.csproj`; `SpruceBeetleInfo.cs`; `SpruceBeetle.sln`; `Properties/Resources.resx`; `Properties/Resources.Designer.cs`; unused `Resources/24x24_{BinPacking,BinPackingPy,ContainerPacking,DirectAlignment}.png` removed; deleted `Documentation/Examples/Insert file path here`; `Documentation/joint-placement.png` (moved from root); `Documentation/README.md`; `Documentation/Component-Reference.md`; `Documentation/Packing-Joints.md`; `Setup/Repo-Cleanup-Todo.md`; `README.md`; `LICENSE`; `Compiled/SpruceBeetle/manifest.yml`; `Compiled/SpruceBeetle/misc/README.md`; `Compiled/SpruceBeetle/misc/LICENSE`. `Alignment/CustomJoints.cs` was deleted then restored (see entry above).
- **Before → after:** Tracked Debug `.gha` / `obj/` and a ~4 MB misnamed JSON → ignored build output (local `bin\Debug\net48\SpruceBeetle.gha` kept on disk). csproj `0.1` vs Yak `1.0.1` → both **1.0.1** (`version: $version`). README/Yak pointed at DominikReisach/Spruce-Beetle → this fork, with original MIT credit and plugin `AuthorName` still Dominik Reisach. No Grasshopper component behavior change.
- **Result / observation:** Source folders left as Grasshopper tabs. Component-Reference packing tables still stale on purpose (see Packing-Joints). Compiled `.gha` remains a snapshot, not source of truth.
- **Follow-ups:** Sync Component-Reference after Contact Tenon ships; optional Excel COM / Git LFS still listed in `Setup/Repo-Cleanup-Todo.md`.

### 2026-09-10 — docs: Packing joints spec (Contact Tenon, two paths)

- **Motivation:** Packed Stacks, Packed Contacts, Select Contacts, and Contact Joints were added without a written contract. Need intended function vs as-built before renaming Contact Joints, adding depth/inset, or changing placement.
- **Files:** `Documentation/Packing-Joints.md`, `Documentation/Thesis-Change-Log.md`
- **Before → after:** No packing-joint spec → working spec: two parallel paths (Stacks → Alignment Tenon kept; Contacts → Contact Tenon); per-component intended / as-built / issues; Contact Joints proposed rename to **Contact Tenon** (`PackTenon`, GUID unchanged); proposed knobs `Dep`, `I`, `Place` (Center / Edge / Seam, default Seam per `joint placement.png`). No plugin code changed.
- **Result / observation:** Seams *filter* (Select Contacts, too loose: 88/88) is a separate bug from Seam *placement* (Contact Tenon). Component-Reference and Column-Fill were left stale on purpose until the Contact Tenon edit ships. Contact Spline named as later only.
- **Follow-ups:** Review the spec; then code pass: rename + `Dep`/`I`/`Place`; then tighten Seams to T-junctions; then sync Component-Reference.

### 2026-09-10 — docs: More varied sizes in column test stock

- **Motivation:** Column packing tests looked repetitive because the 100-row CSVs just cycled a handful of 3/6/12/24" blocks.
- **Files:** `Documentation/TestData/stock_column_in.csv`, `stock_column_ft.csv`, `stock_column_in.json`, `stock_column_ft.json`, `Documentation/Column-Fill-2x2x8.md`
- **Before → after:** Mostly duplicate 6×6, 12×6, 12×12 at 12/24/48/96" → 100 leftover-like pieces (97 unique triples). Sections 2–24", lengths 8–96", including three full-height sticks (4×4×96, 6×8×96, 3×5×96). All still fit a 24×24×96 box with rotation. JSON twins regenerated. Other TestData files unchanged.
- **Result / observation:** Mix is leftover-looking (odd pairs like 5×7, 9×12, 16×16) rather than a stud grid. `stock_column_ft.csv` stays a copy in inches (filename is not a unit converter).
- **Follow-ups:** Re-pack `stock_column_in.csv` / `.json` in Grasshopper; expect a more irregular fill and different contact counts.

### 2026-09-09 — docs: Add JSON twins of TestData stock CSVs

- **Motivation:** JSON to Offcut (`To Offcut`) needs a serialized Offcut array; packing tests should work without CSV to Offcut.
- **Files:** `Documentation/TestData/stock_*.json` (seven files, 100 Offcuts each), matching the CSVs expanded the same day.
- **Before → after:** Stock existed as `index;x;y;z` CSV only → each CSV now has a JSON array of `{ Index, X, Y, Z, Vol }` (inches). No geometry or planes (same as Construct / CSV to Offcut before alignment).
- **Result / observation:** Grasshopper path: Panel with full path → **JSON to Offcut**. `Documentation/Reproduce/offcuts.csv` was not converted.
- **Follow-ups:** Point `Initial_Tests.gh` at `stock_column_in.json` if testing that import path.

### 2026-09-09 — docs: Expand all TestData stock CSVs to 100 offcuts

- **Motivation:** Larger packing tests (column fill and contact joints) need more stock than the original 8–60 row files.
- **Files:** `Documentation/TestData/stock_small.csv`, `stock_uniform.csv`, `stock_varied_length.csv`, `stock_mixed_section.csv`, `stock_packing.csv`, `stock_column_in.csv`, `stock_column_ft.csv`
- **Before → after:** Each file had 8 / 12 / 20 / 12 / 24 / 60 / 60 rows → each now has 100 `index;x;y;z` rows. Extra rows cycle the original sizes; indices are 1–100. Format and units unchanged (inches). `Documentation/Reproduce/offcuts.csv` (Offcut Tales, metres) was not edited.
- **Result / observation:** `stock_column_in.csv` / `stock_column_ft.csv` still use the same lumber mix (3″–24″ sections, 12″–96″ lengths) that fits a 24×24×96 column. Uniform / varied-length / mixed-section / packing files keep their original cross-sections and length sets, repeated.
- **Follow-ups:** Re-run pack + Packed Contacts / Packed Stacks; leftover unused stock is expected if 100 pieces exceed column volume.

### 2026-09-08 — feature: Packed face contacts and tool-sized Contact Joints

- **Motivation:** Packed column needs joints that hold the structure together without a tenon on every face center. Joint shape should follow CNC bit size; placement should sit on sides/seams (see `joint placement.png`), with Packed Stacks kept as a separate Grasshopper option.
- **Files:** `Packing/PackedNeighbors.cs`, `Packing/PackedContact_GH.cs`, `Packing/PackedContacts_GH.cs`, `Packing/SelectContacts_GH.cs`, `Packing/ContactJoints_GH.cs`
- **Before → after:** Only Packed Stacks (Z-columns) feeding Tenon Joints (centered, independent JX/JY/JZ) → new parallel path: **Packed Contacts** (`PackContacts`) lists Z and XY face contacts (planes, overlap rectangles, axis); **Select Contacts** (`PickJoints`) filters All / Z / XY / Seams / Connected; **Contact Joints** (`PackJoints`) cuts matching pockets from tool diameter `D` (fillet `D/2`, width `W×D`, depth `min(D, ~1/3 thinner member)`, inset `D` toward a triple-seam edge). PackBin, Packed Stacks, and Tenon Joints GUIDs unchanged.
- **Result / observation:** Wired on `Initial_Tests.gh` beside the stack path. On `stock_column_in.csv` pack (~33 pieces): 88 contacts (28 Z, 60 XY). **Seams currently keeps all 88** (seam test too loose). Contact Joints: 23 pockets cut, 65 skipped (overlap too small for `D=0.25"` and/or boolean difference failed). Boolean failures are logged as skipped planes; pieces keep the last successful solid.
- **Follow-ups:** Tighten Seams so it matches the red-tick sketch; reduce boolean failures; try Connected vs Z-only; update `Documentation/Component-Reference.md` for the three new Packing components. Optional later: Tenon placement enum (Center / Edge) without replacing this path.

### 2026-09-08 — docs: Backfill thesis log from 2026 git history

- **Motivation:** Reconstruct research notes for work done before the logging rule existed.
- **Files:** `Documentation/Thesis-Change-Log.md`
- **Before → after:** Only the 2026-09-08 baseline entry → reverse-chronological entries for setup, docs, test stock, packing geometry fix, packed Offcut output, and Packed Stacks (2026-08-27 through 2026-09-02).
- **Result / observation:** Source of truth is `git log` on this fork (`Cfree1989/Spruce-Beetle-2.0`), plus existing guides. Original Reisach plugin history before 2026-08-27 is not itemized here. `Documentation/Component-Reference.md` still describes packing as Brep-only and does not document Packed Stacks.
- **Follow-ups:** Update Component-Reference for the new `Oc` packing output and Packed Stacks; decide whether packing should honor the input box pose/center (discussed 2026-09-02, not implemented).

### 2026-09-08 — docs: Start thesis change log and Cursor logging rule

- **Motivation:** Capture every plugin change as research notes for the master’s thesis; stop relying on chat history alone.
- **Files:** `.cursor/rules/thesis-change-log.mdc`, `Documentation/Thesis-Change-Log.md`
- **Before → after:** No structured change log → always-on agent rule requires an entry here after each repo change.
- **Result / observation:** Logging process in place. Prior 2026 work is listed in the entries below (backfilled the same day).
- **Follow-ups:** Keep appending after every subsequent change.

### 2026-09-02 — feature: Packed Offcuts as Offcut objects plus Packed Stacks

- **Motivation:** Column fill needs jointable stacks, not only preview solids. Tenon Joints expects ordered Offcuts with end planes; packing previously dropped that data.
- **Files:** `Packing/BinPackingCS_GH.cs`, `Packing/PackedStacks_GH.cs`, `Packing/PackedNeighbors.cs`
- **Before → after:** EB-AFIT output was Breps only → also outputs Offcuts (`Oc`) with rotated size, Brep, and Z-end planes. New **Packed Stacks** (`PackStacks`) groups face-to-face Z neighbors (tolerance default 0.01), one tree branch per stack of 2+ pieces (bottom to top), plus Isolated pieces and Contact Planes.
- **Result / observation:** Intended Grasshopper path: Bin Packing EB-AFIT → Packed Stacks → graft into Tenon Joints. Packed Stacks reuses the Find Intersections icon. Component-Reference was not updated in this commit.
- **Follow-ups:** Document Packed Stacks and the third packing output; test stacks on `stock_column_in.csv`; confirm Tenon Joints on packed (Z-up) planes vs curve-aligned pieces.

### 2026-09-02 — fix: Packing container size and Z-up coordinate mapping

- **Motivation:** A Rhino-modeled 24×24×96 column packed lying down / wrong size when using Domain Box or a box not sitting on 0–T1 domains.
- **Files:** `Packing/BinPackingCS_GH.cs`
- **Before → after:** Container used interval `T1` as size (wrong if the domain does not start at 0) and mapped EB-AFIT Length/Height/Width straight into Rhino XYZ → now uses axis **lengths** and remaps library Height (Y) ↔ Rhino Z so packing is Z-up.
- **Result / observation:** Packing still builds an origin-aligned World XY box; input box **position and orientation are ignored**. A column centered on the origin in Rhino will not appear at that pose in the packing output.
- **Follow-ups:** Optional later change: place packed solids in the input box frame / by center point (user preference recorded; not done).

### 2026-09-02 — setup: Work PC build loop (DESN-ART121-CF)

- **Motivation:** Same plugin must build on the lab machine, not only the home PC.
- **Files:** `Setup/Grasshopper-Plugin-Dev-Guide.md` (plus local `obj/` path noise in the same commit)
- **Before → after:** Guide had home-PC snapshot only → notes work PC steps 1–7 done, Debug build succeeded, Step 8 (Spruce Beetle tab) still needs a full Rhino restart to confirm.
- **Result / observation:** Two-machine F5/`/netfx` Rhino 8 loop is documented. Confirming the loaded `.gha` is this repo (not a leftover original install) remains a practical issue.
- **Follow-ups:** Confirm Step 8 on the work PC after restart; keep machine-specific paths out of source where possible.

### 2026-08-28 — docs: Inch-only test stock CSVs

- **Motivation:** Column workflow and Rhino files are in inches; mixed or foot-scale numbers in CSVs were easy to misread as plugin unit conversion.
- **Files:** `Documentation/TestData/*.csv`, `Documentation/Column-Fill-2x2x8.md`, `Documentation/Component-Reference.md`
- **Before → after:** Test CSVs mixed scales / earlier metric-like values → all listed test stock files use inches. `stock_column_ft.csv` remains a *named* variant but was rewritten in the same inch-consistency pass (do not treat the filename as a unit converter).
- **Result / observation:** Plugin still does not convert units. CSV, Box, and Rhino document units must match.
- **Follow-ups:** Prefer `stock_column_in.csv` for the 2×2×8 column; avoid typing 2, 2, 8 into Grasshopper.

### 2026-08-28 — docs: 2′ × 2′ × 8′ column-fill workflow

- **Motivation:** First thesis demonstrator path: fill a building column with leftover parts via packing, not curve alignment.
- **Files:** `Documentation/Column-Fill-2x2x8.md`, `Documentation/TestData/stock_column_in.csv`, `Documentation/TestData/stock_column_ft.csv`
- **Before → after:** No written packing recipe → step-by-step: inches, 24×24×96 Domain Box, CSV → CSV to Offcut → Bin Packing EB-AFIT.
- **Result / observation:** Clarified that packing is a volume fill of a box, distinct from Offcut Tales–style alignment along a curve.
- **Follow-ups:** Refresh this guide after Packed Stacks / Offcut packing outputs.

### 2026-08-28 — docs: Component reference and packing test CSVs

- **Motivation:** Need a readable map of every Grasshopper component (inputs/outputs) and stock files for packing tests.
- **Files:** `Documentation/Component-Reference.md`, `Documentation/TestData/stock_small.csv`, `stock_uniform.csv`, `stock_varied_length.csv`, `stock_mixed_section.csv`, `stock_packing.csv`
- **Before → after:** No in-repo component encyclopedia → Create / Alignment / Packing / Fabricate documented from source. Small CSVs added for packing experiments.
- **Result / observation:** Reference still reflects pre–Packed Stacks packing (Brep-only, origin box). That is now stale relative to the 2026-09-02 code.
- **Follow-ups:** Sync Packing section with current `BinPackingCS_GH` and add Packed Stacks.

### 2026-08-27 — setup: Rhino 8 debug host and NuGet packing library

- **Motivation:** Build and F5-debug on this machine without Dominik’s post-build copy path; packing DLL must restore like any other package.
- **Files:** `SpruceBeetle.csproj`, `Properties/launchSettings.json`, `Setup/Grasshopper-Plugin-Dev-Guide.md`
- **Before → after:** Start program pointed at Rhino 7 / Dominik’s `Libraries` copy; packing referenced via `HintPath` to `bin\` → Rhino 8 + `/netfx`, `CromulentBisgetti.ContainerPacking` 1.0.0 as PackageReference, post-build copy removed.
- **Result / observation:** Plugin still targets **.NET Framework 4.8** and Grasshopper NuGet 7.15; Rhino 8 is the host, not an SDK upgrade. `bin/` / `obj/` were committed in this era (cleanup still on the todo list).
- **Follow-ups:** Stop tracking build output (`Setup/Repo-Cleanup-Todo.md` Phase 1).

### 2026-08-27 — setup: Grasshopper plugin build-and-load guide

- **Motivation:** This copy is the thesis working tree; first job is a reliable edit → build `.gha` → Grasshopper tab loop on Windows.
- **Files:** `Setup/Grasshopper-Plugin-Dev-Guide.md`, later `Setup/Repo-Cleanup-Todo.md`
- **Before → after:** No local setup bible → ordered steps, home-PC snapshot (tab loads), git remote confirmed as `Cfree1989/Spruce-Beetle-2.0` (not Reisach’s repo), do-not-touch list (GUIDs, MIT headers, GH category leading spaces).
- **Result / observation:** Development started from a working original plugin, not a blank McNeel template. Cleanup todos are hygiene, not a rewrite.
- **Follow-ups:** Execute gitignore / untrack `bin/` `obj/` when ready for a dedicated cleanup commit.
