# Work timeline, August 27 – October 28, 2026

Reconstructed order of the thesis work. Most of the modeling happened in one Rhino file (`Dowel Model.3dm`) and one Grasshopper file (`Examples.gh`), saved over each other. Neither file records when an object was made. This page puts together the sources that do carry dates.

Each screenshot below comes with the question or problem it was sent with, what the Cursor chat found, and what changed afterward. Newest work is at the bottom of the day-by-day section.

---

## Sources and how far to trust them

| Source | What it dates | How exact |
| --- | --- | --- |
| [Thesis-Change-Log.md](../Thesis-Change-Log.md) | Each plugin feature, fix, and experiment, by day | Day. Written the same day as the change. |
| Git commits | Each committed change | Minute. A commit can come hours after the work. |
| Cursor chat history (27 chats) | Each screenshot's message, and the reply | Minute. Each message carries a timestamp. Chat IDs are given as `chat 1234abcd`. |
| Screenshots in `images/` | What was on screen | Minute. Names with six digits (`2026-10-09_144224`) are the capture time. Names ending in `-pasted` are when the image was pasted into a chat, so the capture was at or before that time. |
| Snapshot files (table below) | The whole model as it was at that save | Minute. Shows only what still existed then. |
| OneDrive version history | The whole file at each synced save | Minute. Listed for `Examples.gh` (71 versions); not yet for `Dowel Model.3dm`. |
| Features visible on a column | The earliest day it could have been made | Day. A "not before" date only. |

A screenshot shows that something existed at that time. It does not show when it was first made.

---

## Rhino and Grasshopper snapshot files

All in `OneDrive - Louisiana State University\Personal\Masters\26 Fall\Spruce Beetle\` unless noted.

Dates as of 3:30 PM on Oct 28. "Created" is the Windows creation date: when that file name first appeared on this computer. It survives later saves.

| File | Created | Last saved | Notes |
| --- | --- | --- | --- |
| `Initial_Tests.gh` | Aug 28, 4:04 PM | Sep 8, 10:20 PM | Created minutes after the last Aug 28 screenshot, so the first column tests were saved here. Packed Contacts and Contact Joints were tested here on Sep 8. |
| `Initial_Tests.3dm` | Aug 28, 4:05 PM | Sep 8, 10:20 PM | Rhino file for the same tests. |
| `Initial_Tests.3dmbak` | Sep 8, 9:11 AM | Sep 8, 10:08 PM | The save before `Initial_Tests.3dm`. |
| `Examples.gh` | Sep 9, 9:22 AM | Oct 28, 3:23 PM | Current working definition. 71 OneDrive versions, below. |
| `Examples.3dm` | Sep 9, 10:40 AM | Sep 14, 4:25 PM | Created the same minute as `Examples.gh` version 4. One OneDrive version. |
| `Examples.3dmbak` | Sep 9, 10:40 AM | Sep 13, 9:55 PM | The save before `Examples.3dm`. |
| `Packed Tenon joints.gh` | Sep 20, 2:25 PM | Sep 19, 3:11 PM | Created after its last save, so it was copied or downloaded onto this computer on Sep 20. Two OneDrive versions, both Sep 19. |
| `Examples_autosave.gh` | Oct 2, 9:30 AM | Oct 2, 9:30 AM | Grasshopper autosave of `Examples.gh`, before that day's orientation lock and Dowel Column. |
| `RhinoAutosave.3dm` | | Oct 6, 10:46 AM | In `%LOCALAPPDATA%\McNeel\Rhinoceros\8.0\AutoSave\`. Rhino's autosave, 14 minutes before `Dowel Model.3dm` was created. |
| `Dowel Model.3dm` | Oct 6, 11:00 AM | Oct 28, 3:23 PM | Current working file. Created the same minute as `Examples.gh` version 53. |
| `Dowel Model.3dmbak` | Oct 6, 11:00 AM | Oct 28, 12:45 PM | The save before the current `Dowel Model.3dm`. |
| Grasshopper autosave `f924a285-….gh` | | Oct 28, 2:53 PM | In `%APPDATA%\Grasshopper\AutoSave\`. |

`Dowel Model.3dm` did not exist before 11:00 AM on Oct 6. Columns from earlier days were made in `Initial_Tests.3dm`, `Examples.3dm`, or a file saved over at that time; the Oct 6 autosave from 10:46 AM is the closest copy of what was open just before.

Copy these somewhere read-only before opening them, so a save does not overwrite the date.

### Other OneDrive version histories

| File | Versions | Notes |
| --- | --- | --- |
| `Examples.3dm` | 1: Sep 14, 4:25 PM, 308 KB | Only the last save, although `Examples.3dmbak` shows it was also saved Sep 13. |
| `Packed Tenon joints.gh` | 1: Sep 19, 2:48 PM, 35.6 KB; 2: Sep 19, 3:11 PM, 14.2 KB | Both during the Contact Tenon work. |
| `Dowel Model.3dm` | not checked yet | |

Rhino files may keep fewer versions than Grasshopper files. When Rhino saves with a backup, it renames the old file to `.3dmbak` and writes a new `.3dm`. OneDrive can then see a new file rather than a new version, and older versions may sit on the `.3dmbak` instead. Grasshopper writes over the same file, so `Examples.gh` kept every save. Check the history on both `Dowel Model.3dm` and `Dowel Model.3dmbak`.

---

## `Examples.gh` versions on OneDrive

OneDrive kept 71 versions of `Examples.gh`, from 9:22 AM on Sep 9 to 3:23 PM on Oct 28. Version 71 is the current file: the local copy was saved at 3:23 PM and is the same 499 KB.
Version 1 is from Sep 9, so earlier work is not in this file. The Aug 28 tests were saved as `Initial_Tests.gh` (created 4:04 PM that day). The Sep 2 Grasshopper window is titled "unnamed" in the screenshots.

To see the definition as it was on a day, open **Version history** on `Examples.gh`, pick that day's last version, and download it to a separate folder.

| Day | Versions | Saved | Size | Work that day |
| --- | --- | --- | --- | --- |
| Sep 9 | 1–12 | 9:22–11:56 AM | 11–24 KB | First saves of this file; test CSVs expanded to 100 offcuts |
| Sep 10 | 13–20 | 10:07 AM – 12:37 PM | 26–30 KB | Packing-joints spec; more varied column stock |
| Sep 13 | 21 | 9:55 PM | 44 KB | |
| Sep 14 | 22–27 | 11:30 AM – 10:53 PM | 24–45 KB | Used Offcuts; Label Offcut Numbers |
| Sep 19 | 28–34 | 11:59 AM – 4:57 PM | 35–52 KB | Contact Tenon |
| Sep 23 | 35–39 | 2:06–3:41 PM | 24–40 KB | Contact Spline; square mouth; planes not baked |
| Sep 25 | 40–43 | 1:59–3:23 PM | 39–42 KB | Outside Key; clearance sliders |
| Sep 30 | 44 | 3:48 PM | 33 KB | 2×4 stud scrap experiments |
| Oct 1 | 45 | 2:19 PM | 44 KB | Scraps under 8 in dropped |
| Oct 2 | 46–50 | 9:39 AM – 1:48 PM | 43–45 KB | Orientation lock; first Dowel Column |
| Oct 5 | 51–52 | 3:06–3:46 PM | 36–46 KB | Locked packing stops bridging holes |
| Oct 6 | 53 | 11:00 AM | 39 KB | Solid Difference for live dowel holes; `Dowel Model.3dm` created this minute |
| Oct 8 | 54–56 | 3:08–3:54 PM | 40 KB, then 492–493 KB | Ranked horizontal dowels; dowels both ways |
| Oct 9 | 57–61 | 2:53–4:16 PM | 487–501 KB | Dowel placement fixes |
| Oct 12 | 62–64 | 12:28–4:17 PM | 499–501 KB | Second, smaller Dowel Column |
| Oct 13 | 65–67 | 3:36–4:29 PM | 504–507 KB | Rod spacing `S` |
| Oct 16 | 68 | 2:54 PM | 504 KB | Stagger Column and Spine Column |
| Oct 27 | 69 | 4:17 PM | 497 KB | Same minute as `Dowel Model.3dmbak` |
| Oct 28 | 70–71 | 12:45 PM, 3:23 PM | 495–499 KB | Bond Column; 12:45 PM is the same minute as `Dowel Model.3dm` |

**Oct 8 size jump.** The file grew from 40 KB (version 54, 3:08 PM) to 492 KB (version 55, 3:38 PM) and stayed near 500 KB afterward. Something large was added to the definition in that half hour, the same time as the "dowels only go one way" screenshot. Internalized geometry is a common cause. Opening versions 54 and 55 side by side would show what it was.

**Rhino and Grasshopper saved together.** Versions 21 and 24 share their minute with `Examples.3dmbak` (Sep 13, 9:55 PM) and `Examples.3dm` (Sep 14, 4:25 PM). Versions 69 and 70 share their minute with `Dowel Model.3dmbak` and `Dowel Model.3dm`. The Rhino file's OneDrive history probably lines up with this list.

<details>
<summary>Every version, as listed in OneDrive</summary>

| Version | Saved | Size |
| --- | --- | --- |
| 1 | Sep 9, 9:22 AM | 12.4 KB |
| 2 | Sep 9, 9:36 AM | 11.3 KB |
| 3 | Sep 9, 10:18 AM | 14.9 KB |
| 4 | Sep 9, 10:40 AM | 18.3 KB |
| 5 | Sep 9, 11:08 AM | 18.9 KB |
| 6 | Sep 9, 11:16 AM | 16.6 KB |
| 7 | Sep 9, 11:16 AM | 16.6 KB |
| 8 | Sep 9, 11:17 AM | 16.9 KB |
| 9 | Sep 9, 11:21 AM | 16.9 KB |
| 10 | Sep 9, 11:39 AM | 19.5 KB |
| 11 | Sep 9, 11:54 AM | 22.4 KB |
| 12 | Sep 9, 11:56 AM | 23.9 KB |
| 13 | Sep 10, 10:07 AM | 27.4 KB |
| 14 | Sep 10, 10:28 AM | 29.2 KB |
| 15 | Sep 10, 10:39 AM | 28.4 KB |
| 16 | Sep 10, 11:51 AM | 28.3 KB |
| 17 | Sep 10, 11:51 AM | 26.2 KB |
| 18 | Sep 10, 12:20 PM | 26.7 KB |
| 19 | Sep 10, 12:24 PM | 27.4 KB |
| 20 | Sep 10, 12:37 PM | 29.7 KB |
| 21 | Sep 13, 9:55 PM | 43.5 KB |
| 22 | Sep 14, 11:30 AM | 23.6 KB |
| 23 | Sep 14, 12:13 PM | 40.3 KB |
| 24 | Sep 14, 4:25 PM | 45.1 KB |
| 25 | Sep 14, 4:48 PM | 24.2 KB |
| 26 | Sep 14, 10:35 PM | 43.4 KB |
| 27 | Sep 14, 10:53 PM | 24.2 KB |
| 28 | Sep 19, 11:59 AM | 46.6 KB |
| 29 | Sep 19, 12:16 PM | 46.8 KB |
| 30 | Sep 19, 12:29 PM | 50.2 KB |
| 31 | Sep 19, 4:16 PM | 49.1 KB |
| 32 | Sep 19, 4:28 PM | 51.9 KB |
| 33 | Sep 19, 4:46 PM | 38.4 KB |
| 34 | Sep 19, 4:57 PM | 35.1 KB |
| 35 | Sep 23, 2:06 PM | 23.8 KB |
| 36 | Sep 23, 2:36 PM | 31.9 KB |
| 37 | Sep 23, 2:54 PM | 35.9 KB |
| 38 | Sep 23, 3:39 PM | 40.4 KB |
| 39 | Sep 23, 3:41 PM | 38.0 KB |
| 40 | Sep 25, 1:59 PM | 41.5 KB |
| 41 | Sep 25, 2:38 PM | 41.7 KB |
| 42 | Sep 25, 2:56 PM | 38.8 KB |
| 43 | Sep 25, 3:23 PM | 39.6 KB |
| 44 | Sep 30, 3:48 PM | 33.2 KB |
| 45 | Oct 1, 2:19 PM | 43.8 KB |
| 46 | Oct 2, 9:39 AM | 44.6 KB |
| 47 | Oct 2, 10:30 AM | 42.9 KB |
| 48 | Oct 2, 10:30 AM | 42.9 KB |
| 49 | Oct 2, 11:24 AM | 42.7 KB |
| 50 | Oct 2, 1:48 PM | 44.8 KB |
| 51 | Oct 5, 3:06 PM | 45.6 KB |
| 52 | Oct 5, 3:46 PM | 35.7 KB |
| 53 | Oct 6, 11:00 AM | 38.9 KB |
| 54 | Oct 8, 3:08 PM | 40.3 KB |
| 55 | Oct 8, 3:38 PM | 492 KB |
| 56 | Oct 8, 3:54 PM | 493 KB |
| 57 | Oct 9, 2:53 PM | 487 KB |
| 58 | Oct 9, 3:47 PM | 498 KB |
| 59 | Oct 9, 4:06 PM | 498 KB |
| 60 | Oct 9, 4:11 PM | 500 KB |
| 61 | Oct 9, 4:16 PM | 501 KB |
| 62 | Oct 12, 12:28 PM | 499 KB |
| 63 | Oct 12, 3:37 PM | 501 KB |
| 64 | Oct 12, 4:17 PM | 501 KB |
| 65 | Oct 13, 3:36 PM | 504 KB |
| 66 | Oct 13, 4:07 PM | 507 KB |
| 67 | Oct 13, 4:29 PM | 507 KB |
| 68 | Oct 16, 2:54 PM | 504 KB |
| 69 | Oct 27, 4:17 PM | 497 KB |
| 70 | Oct 28, 12:45 PM | 495 KB |
| 71 | Oct 28, 3:23 PM | 499 KB |

</details>

---

## Dating a column by what it shows

A column cannot be older than the first plugin feature it uses.

| Visible on the column | Not made before |
| --- | --- |
| Packed solids filling the column box | Sep 2 (container size and Z-up fix, committed 10:11 PM) |
| Face contacts or Contact Joints pockets | Sep 8 |
| Engraved scrap numbers | Sep 14 |
| Contact Tenon pockets sized by tool diameter | Sep 19 |
| Contact Spline (edge-open slots) | Sep 23 |
| Outside Key (face keys on the outer skin) | Sep 25 |
| Every piece upright (Longest Z) | Oct 2 |
| No piece sitting over a hole | Oct 5 |
| Horizontal dowels through the wide faces | Oct 8 |
| Dowels that reach every board, dowel stick-out only on open ends | Oct 9 |
| A second, smaller dowel diameter | Oct 12 |
| Several rods at one sideways position, spaced up the height | Oct 13 |
| Neighboring ends deliberately staggered (Stagger Column) or long spines at the corners (Spine Column) | Oct 16 |
| Seams offset layer to layer in plan (Bond Column) | Oct 28, 12:32 PM build |

---

## Day by day

### Aug 27–28 — Build setup and the first column pack

- **Aug 27:** Build-and-load guide; Rhino 8 debug host; container-packing library added. Commits 8:48–10:06 PM.
- **Aug 28:** Component reference, column-fill workflow (24 × 24 × 96 in), inch-only test stock.

#### 2:47 PM — The column volume

![](images/2026-08-28_1447-pasted.png)

- **Shows:** An empty box in Rhino, the column to fill.
- **Asked:** Write a step-by-step document for making a building column: a rectangular 2′ × 2′ × 8′ volume filled with offcuts.
- **Outcome:** `Column-Fill-2x2x8.md` and a matching stock CSV. *(chat c5aa7417)*

#### 2:52 PM — CSV path was still the placeholder

![](images/2026-08-28_1452-pasted.png)

- **Shows:** The panel feeding CSV to Offcut still reads `Insert path to: \offcuts.csv`; the component is red.
- **Found:** That text is a placeholder, not a path. The panel needs the full Windows path to the CSV. The file itself is `index;x;y;z`, semicolons, no header, no feet-and-inches notation. *(chat c5aa7417)*

#### 2:56 PM — The offcut list loaded

![](images/2026-08-28_1456-pasted.png)

- **Shows:** 59 rows of `SpruceBeetle.Offcut`.
- **Found:** That is correct. A panel cannot print the sizes of a custom type; Deconstruct Offcut shows x, y, z. Next step was wiring the list into Bin Packing with the 24 × 24 × 96 box. *(chat c5aa7417)*

#### 3:00 PM — Wrong plugin on screen

![](images/2026-08-28_1500-pasted.png)

- **Shows:** A green upright box and a red box lying along the floor.
- **Found:** This canvas was a Logison definition (CSV 2 Box), not Spruce Beetle. Those boxes came from that plugin and its own CSV format. Back to the Spruce Beetle canvas with `stock_column_in.csv`. *(chat c5aa7417)*

#### 3:58 PM — Packed pieces lie on the floor beside the column

![](images/2026-08-28_1558-pasted.png)

- **Shows:** The packed pieces form a row along the ground next to the tall column instead of filling it.
- **Issue:** "Same outcome" after rewiring.
- **Found:** The 8 ft axis was ending up on the ground. Not solved this day; see Sep 2. *(chat c5aa7417)*

### Sep 2 — Why the pack lies sideways

- Work PC build loop. Fix for container size and Z-up mapping (commit 10:11 PM). Packed pieces output as Offcut objects; Packed Stacks added (commit 10:28 PM).

#### 1:55 PM — Column centered on the origin, output still perpendicular

![](images/2026-09-02_135236.png)
![](images/2026-09-02_135422.png)

- **Shows:** The column solid standing on Z; the packed pieces (red) lying along Y.
- **Issue:** The column is centered at the origin and set as the input, and the output solids are always perpendicular to it.
- **Found:** Bin Packing only reads the box's three sizes and always rebuilds on World XY. A box taken from a Brep often comes from a side face, so the 96 in side lands on a floor axis. Position and orientation of the input are ignored. A follow-up question settled on keeping the output at the origin. *(chat bfe52c56)*

#### 2:03 PM — What a domain is

![](images/2026-09-02_140251.png)

- **Shows:** Setting a Domain parameter to `0-24`.
- **Asked:** Explain "X 0–24, Y 0–24, Z 0–96" for a Grasshopper beginner.
- **Answer:** A domain is a start and an end. `0-24` is 24 in from 0 to 24. Three domains plus a plane build the box from the origin corner to (24, 24, 96). *(chat bfe52c56)*

#### 3:07 PM — How a domain knows its axis

![](images/2026-09-02_1507-pasted.png)

- **Shows:** Three Domains into a box component that is red; tooltip `0 To 96`.
- **Answer:** It does not know. The axis is whichever box input it is wired into (X, Y, or Z), and the plane on `B` decides which way those point in Rhino. *(chat bfe52c56)*

#### 3:09 PM — Finding the right Box

![](images/2026-09-02_150928.png)
![](images/2026-09-02_150934.png)

- **Shows:** The Params → Geometry and Params → Primitive menus.
- **Issue:** "That doesn't exist?" and "I'm not seeing Box under Primitive."
- **Found:** Params → Geometry → Box only holds a box. The one that builds a box from a plane and three domains is **Domain Box**, under Surface → Primitive. *(chat bfe52c56)*

#### 3:14 PM — Same result with Domain Box

![](images/2026-09-02_1512-pasted.png)

- **Shows:** Domain Box tall and green on Z; the pack still red along the floor.
- **Issue:** Same result. The user asked for exact component names and Context7 docs.
- **Found / mistake:** The reply blamed a Brep wired into Domain Box `B`, based on an older screenshot. *(chat bfe52c56)*

#### 3:17 PM — No Brep in the definition

![](images/2026-09-02_1517-pasted.png)

- **Shows:** XY Plane and three Domains into Domain Box, then into PackBinC#. No Brep.
- **Outcome:** The reply apologized for reading an old screenshot and checked which Domain held `0 To 96`. *(chat bfe52c56)*

#### 3:24 PM — The real cause: a Y-up library in a Z-up Rhino

![](images/2026-09-02_1524-pasted.png)

- **Shows:** Wiring correct; the pack still runs along the floor.
- **Found:** A bug in the packer, not the wiring. The packing library is Y-up (height is its Y). The component copied its coordinates straight into Rhino, which is Z-up, so the 96 in height was drawn along world Y. Suggested workaround: Orient the packed solids from XY Plane to XZ Plane. *(chat bfe52c56)*

#### 3:52 PM — Bounding Box does not change it

![](images/2026-09-02_1552-pasted.png)
![](images/2026-09-02_155210.png)

- **Shows:** CSV to Offcut → PackBinC# with the box from Bounding Box; red column, green pack along the floor.
- **Issue:** Result is the same.
- **Found:** Nothing on the box input can stand the pack up; Orient has to go after the packer.
- **Outcome:** Fixed in code that evening (commit 10:11 PM): the container uses axis lengths, and the library's height is remapped to Rhino Z. *(chat bfe52c56)*

### Sep 8–12 — Contacts, joints, change log

- **Sep 8:** Packed face contacts and tool-sized Contact Joints (commit 10:21 PM). Change log started and backfilled. `Initial_Tests` files saved 10:20 PM.
- **Sep 9:** Test CSVs expanded to 100 offcuts; JSON twins added.
- **Sep 10:** Packing-joints spec (Contact Tenon, two paths); more varied column stock.
- **Sep 12:** Repo cleanup; Custom Joints restored.

#### Sep 9, 8:44 AM — Work on two computers

![](images/2026-09-09_0844-pasted.png)

- **Shows:** VS Code Source Control with 22 changed files, and GitHub one commit ahead.
- **Issue:** Uncommitted work on the work PC from the day before, and a commit pushed from home that night.
- **Found:** GitHub already had the finished work. Do not commit the 22 files; stash them (including untracked), then pull. *(chat f6bd3bef)*

#### Sep 9, 8:54 AM — Pull failed after the stash

![](images/2026-09-09_0854-pasted.png)

- **Shows:** The Git error dialog after pulling.
- **Found:** The stash worked. The pull stopped halfway because Rhino had `SpruceBeetle.gha` open, which put some files back. After closing Rhino the pull went through. Leave the stashes alone rather than popping them. *(chat f6bd3bef)*

### Sep 14 — Used Offcuts and engraved numbers

- Used Indices, renamed Used Offcuts, sorted numerically. Label Offcut Numbers: Rhino dots first, then engraving on the largest vertical face, then every digit of two-digit numbers, then a preference for exposed faces. `Examples.3dm` saved 4:25 PM.

### Sep 19 — Contact Tenon

- Contact Tenon takes tool diameter as a mill constraint, matches Alignment Tenon's settings, and skips undersized faces (`SkC`). Packed Stacks deleted after the adapter into Alignment Tenon failed. Select Contacts loses Seams and Connected modes. `Packed Tenon joints.gh` saved 3:11 PM.

### Sep 23 — Contact Spline

- Edge-open keys cut after stacking.

#### 2:53 PM — Spline mouth should be square

![](images/2026-09-23_145229.png)

- **Shows:** A spline slot at a board seam, with a rounded end where it opens on the edge.
- **Issue:** The ends of splines should be 90 degrees to the edge, not curving.
- **Outcome:** The tool-radius fillet now goes only on the closed stop; the mouth is square so a rectangular key slides in. *(chat d1fc19cd)*

#### 3:34 PM — Contact planes and half-drawn splines

![](images/2026-09-23_153326.png)
![](images/2026-09-23_153332.png)
![](images/2026-09-23_153355.png)

- **Shows:** A packed column with engraved numbers (44, 25, 82, 42), tenon pockets on top faces, spline slots, yellow contact planes and green previews; the last view lifts pieces apart.
- **Issue:** The contact planes are useful but should never bake. The splines are built, but only a short piece renders and the rest is a stray line.
- **Outcome:** Bake skips planes. The spline key now spans the slot from the open edge to the stop. *(chat a168eadb)*

### Sep 25 — Outside Key

- Outside Key cuts face keys on the column skin. Clearance slider on every joint cutter.

#### 2:00 PM — Splines open on the top and inner steps

![](images/2026-09-25_135519.png)
![](images/2026-09-25_135851.png)

- **Shows:** The column top with key mouths along the seams on the lid and at steps (pieces 43, 44, 25, 58, 82, 49); the "Packing with Contact Tenons" Grasshopper group.
- **Issue:** Contact Spline does not tell the inside of the column from the outside. The goal is a key on the outside holding the parts together.
- **Found:** The mouth search tries straight up first on a vertical seam and only checks for other scraps, never the column skin. So the keys open on the top face and on inner steps. *(chat 22ef11ed)*

#### 2:08 PM — Keys on the skin, across the seams

![](images/2026-09-25_135519-2.png)

- **Shows:** The same view with blue sketches of bowtie and straight keys crossing seams on the outer faces.
- **Asked:** Keys like this, all on the outside overlapping the seams; the bowtie is optional.
- **Found:** These are face keys: a pocket on the outer face, centered on the seam, half in each piece. They only work where both pieces share the same flat outer face; the step where 43 sits back from 58 cannot take one.
- **Outcome:** Outside Key component, same day. *(chat 22ef11ed)*

### Sep 30 – Oct 1 — Scrap pile experiments

- 100 dressed 2×4 stud scraps as packing stock; a short-scrap pile under 20 in; scraps under 8 in dropped.

### Oct 2 — One orientation per column

- Orientation lock on Bin Packing (Unlimited, Longest Z, Shortest Z). First Dowel Column (vertical dowels, later replaced). `Examples_autosave.gh` from 9:30 AM is the state before this.

#### 1:26 PM — Upright sticks and flat boards in one column

![](images/2026-10-02_132513.png)
![](images/2026-10-02_132516.png)

- **Shows:** A column of upright pieces, and the base of a column with a stack of flat boards on one face beside upright pieces.
- **Asked:** An option to put every longest side on Z, or every thickness on Z. Both look good, but not in the same column.
- **Outcome:** New `Or` input on Bin Packing: Unlimited, Longest Z, Shortest Z. *(chat 9585e888)*

### Oct 5–6 — No more bridging, live dowel holes

#### Oct 5, 2:55 PM — Gaps in a locked column

![](images/2026-10-05_1455-pasted.png)

- **Shows:** Side view of a Longest Z column: bands of upright pieces with gaps and overhangs between them.
- **Asked:** Is this fixable, or a limit of not using unlimited rotation?
- **Found:** The packer fills in flat courses as tall as the tallest piece in each, so shorter boards leave air that nothing can drop into. Seating each board on the wood directly under it removes the floating bands; a ragged top stays.
- **Outcome:** Locked packing places each piece on the floor or a full platform and never over a hole. Shelf air went to 0 on the test stock. *(chat 9585e888)*

#### Oct 6, 10:37 AM — Dowels that cut the blocks and follow them

![](images/2026-10-06_1037-pasted.png)

- **Shows:** Upright numbered boards (55, 79, 78, 85, 77) with short horizontal pins.
- **Asked:** A simple way to make all these dowels cut the blocks in Grasshopper, and update when a dowel moves.
- **Answer:** Solid Difference with the blocks grafted into `A` and the dowels flattened into `B`. Keep the dowels referenced, not baked or internalized, and make them closed solids that stick out past the faces. *(chat 9585e888)*
- **Next:** `Dowel Model.3dm` was created at 11:00 AM, about 20 minutes later, the same minute as `Examples.gh` version 53.

### Oct 8 — Ranked horizontal dowels

#### 2:38 PM — The dowel idea

![](images/2026-10-08_141138.png)

- **Shows:** Numbered boards (57, 51, 52, 59, 61, 75, 74) with purple dowels placed by hand.
- **Idea:** Pack first, then run dowels through the column only through the widest faces where boards overlap. Each part sits on the CNC bed largest face down, so the dowel is perpendicular to that face. Rank lines by how many properly oriented faces they pass through, pick as many as wanted, and set the dowel size.
- **Outcome:** New Dowel Column that ranks horizontal dowels through the packed stack. The Oct 2 version was hidden as Legacy Dowel Placer. *(chat c41399ab)*

#### 3:40 PM — Dowels only go one way

![](images/2026-10-08_1538-pasted.png)

- **Shows:** A column with dowel holes on one set of faces.
- **Issue:** Dowels only run one direction, and the stack on the right has nothing holding it. Proposal: run them both ways, and let a dowel stop at a short side instead of going through the whole column.
- **Outcome:** The dowel count is split across both horizontal axes. A piece with no dowel gets a shorter rod that stops at the far short side of the first doweled piece it meets. *(chat c41399ab)*

### Oct 9 — Dowel placement fixes

Seven commits, 2:53–4:12 PM.

#### 1:36 PM — Why not lower, through all the parts?

![](images/2026-10-09_133435.png)

- **Shows:** A green dowel along the top of the boards; a blue line drawn lower.
- **Issue:** The dowel should go a bit lower, through the blue line, and hit all the parts.
- **Found:** The rod sits at the center of the patch that pierces the most pieces, and a parallel line closer than one hole diameter is dropped. The blue line was a different set of pieces that lost the count. *(chat 62d04991)*

#### 1:54 PM — Room for a dowel, and a hole through a wide board's edge

![](images/2026-10-09_1338-pasted.png)
![](images/2026-10-09_134825.png)

- **Shows:** One board selected with short dowels and one long rod through it; a front wireframe with hole circles and a blue mark.
- **Issue:** The boards overlap a lot at the blue mark and a dowel would fit there. A very wide board has a dowel running through its short edge from side to side.
- **Outcome:** A tie-in rod can no longer enter a board through its narrow edge. *(chat 62d04991)*

#### 2:23–2:27 PM — Overlap, intended spot, actual spot

![](images/2026-10-09_141903.png)
![](images/2026-10-09_141903-2.png)

- **Shows:** Orange is where the parts overlap, blue is where the dowel should go, purple is where it was placed.
- **Found:** A thin band above the orange threaded more pieces and won the ranking.
- **Outcome:** The large shared patch now wins over a thin band, and the rod goes to the center of the orange. *(chat 62d04991)*

#### 2:43 PM — The top dowel needs to drop a few inches

![](images/2026-10-09_144134.png)
![](images/2026-10-09_144224.png)
![](images/2026-10-09_144231.png)
![](images/2026-10-09_144239.png)

- **Shows:** Transparent boards with purple rods, from several angles.
- **Issue:** The top dowel just needs to go down a few inches to hit one more board.
- **Outcome:** A high rod now drops just far enough to enter the next board; lower rods stay put. *(chat 62d04991)*

#### 2:59 PM — No top dowel, and rods too close to the edge

![](images/2026-10-09_145632.png)
![](images/2026-10-09_145639.png)

- **Shows:** The column with rods and holes visible in an opening; a close-up of dowels sticking out past the end boards.
- **Issue:** Now there is no dowel up top, and the rods it does place are too close to the board edges. Keep at least one dowel width from the edge, set by an input.
- **Outcome:** A rod only steps to the nearest height that adds a board, so one stays near the top. New `Edge` input (default one diameter of wood outside the hole). Later that day: stick-out only on open ends, and the clearance hole as a separate `H` output. *(chat 62d04991)*

### Oct 12–13 — Second dowel size and rod spacing

#### Oct 12, 3:26 PM — Overlapping dowels

![](images/2026-10-12_1526-pasted.png)

- **Shows:** Dowel solids only, two of them on top of each other.
- **Asked:** What would keep them from overlapping like this?
- **Found:** Inside one Dowel Column, a rod within one hole diameter of a kept rod is dropped. Two components do not know about each other. Previewing both `D` and `H` also draws every rod twice. *(chat 62d04991)*

#### Oct 12, 3:37 PM — Big dowels first, then small ones where they don't fit

![](images/2026-10-12_1537-pasted.png)
![](images/2026-10-12_1537-pasted-2.png)

- **Shows:** The "Dowel Column + Z Tenons" Grasshopper group with two Dowel Column components, and the dowels in Rhino.
- **Asked:** The large dowels come from the first component; the same component makes smaller dowels where the large ones could not go. Does the wiring look right?
- **Found:** The second component only avoids the first one's rods if its `C` input takes the first one's `SkC`. Otherwise it places small dowels through the whole column. Solid-difference the `H` outputs, not `D`. *(chat 62d04991)*

#### Oct 12, 3:49 PM — Second pass rewired

![](images/2026-10-12_1549-pasted.png)

- **Shows:** Two Dowel Columns (0.375 in, count 60; 0.271 in, count 11); `SkC` of the first into `C` of the second; both hole sets into Solid Difference. Not sent in a chat.

#### Oct 13, 3:28 PM — Spacing rods up a tall board

![](images/2026-10-13_1527-pasted.png)

- **Shows:** A tall column (boards 86, 85) with one hole near the top and one at the middle of a board about 6 ft long.
- **Asked:** A slider for the spacing between dowels along Z, based on the same center, skipping any that don't fit.
- **Outcome:** New `S` input repeats a rod at center ± S, ± 2S, …; stations where the hole does not fit, or that crowd an existing rod, are skipped. *(chat 62d04991)*

### Oct 16 — Columns split into sections

- Stagger Column and Spine Column packers. 250-part test files.

#### 11:03–11:18 AM — Sections that barely touch

![](images/2026-10-16_1103-pasted.png)
![](images/2026-10-16_111736.png)
![](images/2026-10-16_111739.png)

- **Shows:** A column of engraved boards (57, 58, 51, 52, 53, 59, 61, 70, 75, 74, 93, 72, 89) with dowel holes; one tall board selected and then lifted out to show a long piece running most of the height.
- **Issue:** With some scrap lengths the column becomes four sections that are joined inside themselves but barely to each other. Either vary the lengths so the sections stagger into each other, or always run one to four long parts most of the column height. Outside keys are another option, but another part to mill and probably not strong.
- **Outcome:** Both approaches as separate components so they can be compared: Stagger Column and Spine Column. *(chat 399fa8dd)*

### Oct 26–27 — Planning and real lumber

- **Oct 26:** Spring 2027 thesis calendar and planning folder (no model work).
- **Oct 27:** Cut list from the bought 2×4s and two 2×6 × 8 ft boards. `Dowel Model.3dmbak` saved 4:17 PM.

### Oct 28 — Brick bond in plan

- Bond Column added (commit 12:49 PM; build loaded from 12:32 PM). `Dowel Model.3dm` and `Examples.gh` saved 12:45 PM.

#### 11:23–11:40 AM — Columns form two or more bodies

![](images/2026-10-28_112318.png)
![](images/2026-10-28_112323.png)
![](images/2026-10-28_1140-pasted.png)

- **Shows:** Top views of packed columns, each split along a straight seam (one with a separate strip and holes, one a 2 × 5 grid); an elevation looking along the dowels with ends staggered up the column (22, 89, 15, 73, 72, 46, 36).
- **Issue:** The columns always pack into two or more bodies with no connection. Parts only join through their largest faces, and none of those face across the split. The column needs a brick formation in X, Y, and Z. The elevation is the good example: boards staggered vertically, dowels perpendicular to that view.
- **Found:** The locked packers turn every board the same way on a grid, so each seam inside a layer runs straight through every layer. Boards across it only meet edge to edge, and Dowel Column does not drill into a narrow edge.
- **Outcome:** Bond Column keeps all wide faces pointing one way and offsets the seams layer to layer in plan, with ends still staggered. *(chat 01c0a913)*

#### 11:28 AM — Dowel holes on a column face

![](images/2026-10-28_112828.png)

- **Shows:** Elevation of numbered boards (51, 49, 61, 68, 16) with dowel holes. Not sent in a chat.

#### 12:35 PM — Stagger vs Bond, plan sections

![](images/2026-10-28_1235-render-stagger-small.png)
![](images/2026-10-28_1235-render-bond-small.png)
![](images/2026-10-28_1235-render-bond-column.png)

- **Shows:** Plan sections at three heights rendered from the packers outside Rhino: Stagger Column in a 7 × 7.5 box (8 separate groups), Bond Column in the same box (one group), and Bond Column in the 24 × 24 × 96 column. *(chat 01c0a913)*

---

## Gaps to fill

- **`Dowel Model.3dm` version history.** Open **Version history** on `Dowel Model.3dm` and on `Dowel Model.3dmbak`. It holds the baked columns from Oct 6 on, so its versions date the geometry itself. Download one version per working day into a separate folder.
- **What changed on Oct 8 between versions 54 and 55.** Open both copies to see what made the file 450 KB larger.
- **Days with commits but no screenshots:** Sep 8, 14, 19, Sep 30 – Oct 1, Oct 27. The snapshot files above cover Sep 8, 14, and 19.
- **`RhinoAutosave.3dm` (Oct 6):** Open a copy and check which file it was.
- **Columns in `Dowel Model.3dm`:** Match each one against the feature table to give it a "not before" date, then narrow it with the OneDrive versions.

Screenshots left out because they are not model work: Fusion 360 and Rhino navigation settings (Oct 2), a laptop thermal-putty product page (Oct 9), and a blank calendar template used for the printable thesis calendar (Oct 26).

---

## Going forward

- Use Rhino's `IncrementalSave` at each milestone (`Dowel Model 001.3dm`, `002`, …), and save a numbered copy of the Grasshopper file at the same time.
- Bake each iteration onto a layer named with the date and what changed, e.g. `2026-10-28 Bond Column St4 Bo1.5`.
- Add a screenshot to `images/` with the date and time in the name whenever a result is worth showing.
