# Thesis Change Log

Working lab notebook for the master’s thesis on the Spruce Beetle Grasshopper plugin. This is **not** a user-facing changelog.

Original plugin: Dominik Reisach, *Spruce Beetle*. This repo (`Spruce-Beetle-2.0`) is a working copy for thesis development.

---

## Active research focus

<!-- Update this when the research question or primary workflow changes. The agent reads this when writing Motivation. -->

Pack leftover rectangular offcuts into a **2′ × 2′ × 8′ (24″ × 24″ × 96″) column** with Bin Packing EB-AFIT (Rhino inches), then group Z-stacks so Tenon Joints can run on packed (not curve-aligned) pieces.

Related guides already in the repo:

- [Component-Reference.md](Component-Reference.md)
- [Column-Fill-2x2x8.md](Column-Fill-2x2x8.md)
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
