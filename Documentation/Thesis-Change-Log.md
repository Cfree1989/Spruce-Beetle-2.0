# Thesis Change Log

Working lab notebook for the master’s thesis on the Spruce Beetle Grasshopper plugin. This is **not** a user-facing changelog.

Original plugin: Dominik Reisach, *Spruce Beetle*. This repo (`Spruce-Beetle-2.0`) is a working copy for thesis development.

---

## Active research focus

<!-- Update this when the research question or primary workflow changes. The agent reads this when writing Motivation. -->

Pack leftover rectangular offcuts into a **2′ × 2′ × 8′ (24″ × 24″ × 96″) column** with Bin Packing EB-AFIT (Rhino inches). Two joint options (keep both): **Packed Stacks → Tenon Joints** (Z-columns, centered), or **Packed Contacts → Select Contacts → Contact Tenon** (today still named Contact Joints; Z + XY, pocket centered on each shared overlap rectangle). Spec: [Packing-Joints.md](Packing-Joints.md). Code rename and extra knobs are not shipped yet.

Related guides already in the repo:

- [README.md](README.md) — what each doc is for
- [Packing-Joints.md](Packing-Joints.md)
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
