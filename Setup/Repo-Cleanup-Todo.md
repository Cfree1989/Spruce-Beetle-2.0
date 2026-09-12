# Spruce Beetle — Repo Cleanup Todo

Hygiene pass completed **2026-09-12**. Source folders (`Alignment/`, `Base/`, `Create/`, `Packing/`, `Fabricate/`) were left as-is.

**Related:** [Grasshopper-Plugin-Dev-Guide.md](Grasshopper-Plugin-Dev-Guide.md) (build/load loop).

---

## Do not touch

Leave these alone unless you later decide they are a real product problem.

- **Never** change component GUIDs (existing `.gh` files bind to them)
- **Never** strip MIT copyright headers (`Copyright (c) 2022 Dominik Reisach`)
- **Never** rewrite git history to drop old blobs
- Do not “upgrade” Grasshopper NuGet 7.15 → Rhino 8 SDK just to clean up
- Do not remove leading spaces in GH category names (`"    Alignment"`) — that is tab sort order
- Do not delete `Documentation/Examples/` or `Documentation/Reproduce/` (large `.3dm` / `.obj` are legitimate docs)
- Do not treat `TestAlignment` (obscure) or `ListUpdate` (hidden) as dead code
- **Never** delete Grasshopper component files, including commented-out ones (`CustomJoints`)

---

## Done (2026-09-12)

- Expanded `.gitignore` (VS / .NET + Rhino); untracked `bin/`, `obj/`, `.DS_Store` without deleting the local Debug `.gha`
- Aligned plugin / csproj / Yak version to **1.0.1** (`manifest.yml` uses `$version`)
- `Alignment/CustomJoints.cs` kept (commented-out Custom Joints; restored after a delete — do not remove component files)
- Removed empty Solution Items from `SpruceBeetle.sln`
- Removed unused icons (`BinPacking`, `BinPackingPy`, `ContainerPacking`, `DirectAlignment`) and phantom csproj `None Remove` entries
- Deleted misnamed `Documentation/Examples/Insert file path here` JSON blob
- Moved `joint placement.png` → `Documentation/joint-placement.png`
- Added [Documentation/README.md](../Documentation/README.md); banner on Component-Reference pointing at Packing-Joints
- README / LICENSE / Yak metadata describe this fork; Dominik MIT credit and plugin `AuthorName` kept
- `Compiled/*.gha` and DLL remain a load-without-build snapshot, not source of truth

Skipped on purpose: renaming `24x24_Deconstruct Offcut.png` (space in filename).

---

## Later / optional (not cleanup)

- Isolate or replace Excel COM (`ClosedXML` / `ExcelDataReader`) so the plugin builds without Office
- Multi-target `net48;net7.0` for Rhino 8 default runtime (Setup guide Step 0 — skip until the tab is boring)
- Git LFS for huge docs (`sprucebeetle_examples.3dm` ~89 MB, `offcut_tales.obj` ~88 MB) if clones hurt
- Work PC: Office missing may fail the Excel COM build (see setup guide blockers)
- Sync [Component-Reference.md](../Documentation/Component-Reference.md) packing tables after Contact Tenon ships
- Plugin `AuthorName` / Yak `authors`: list both names when publishing under the fork author’s name
