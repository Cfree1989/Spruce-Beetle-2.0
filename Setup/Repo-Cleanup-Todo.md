# Spruce Beetle — Repo Cleanup Todo

Work through this **in order**. The plugin already builds and the Grasshopper tab loads. This list is hygiene, not a rewrite.

Do **not** start this until you want a dedicated cleanup commit (or a few small ones). Do **not** mix this with component-logic changes.

Sources: first-pass audit (2026-08-27), Git / .NET gitignore templates, McNeel Yak packaging docs, MIT fork attribution practice.

**Related:** [Grasshopper-Plugin-Dev-Guide.md](Grasshopper-Plugin-Dev-Guide.md) (build/load loop). That file currently has **uncommitted local edits** (home vs work PC notes). Commit or stash those before a cleanup commit so the diffs stay separate.

---



## Do not touch

Leave these alone unless you later decide they are a real product problem.

- [ ] **Never** change component GUIDs (existing `.gh` files bind to them)
- [ ] **Never** strip MIT copyright headers (`Copyright (c) 2022 Dominik Reisach`)
- [ ] **Never** rewrite git history to drop old blobs
- [ ] Do not “upgrade” Grasshopper NuGet 7.15 → Rhino 8 SDK just to clean up
- [ ] Do not remove leading spaces in GH category names (`"    Alignment"`) — that is tab sort order
- [ ] Do not delete `Documentation/Examples/` or `Documentation/Reproduce/` (large `.3dm` / `.obj` are legitimate docs)
- [ ] Do not treat `TestAlignment` (obscure) or `ListUpdate` (hidden) as dead code

---



## Phase 1 — Gitignore and stop tracking build output

**Do this first.** Standard C# practice. Highest value, lowest risk.

Use the official templates, then untrack files that are already in git:

- [VisualStudio.gitignore](https://github.com/github/gitignore/blob/main/VisualStudio.gitignore)
- [Dotnet.gitignore](https://github.com/github/gitignore/blob/main/Dotnet.gitignore)
- or `dotnet new gitignore`
- macOS: `.DS_Store` (not `*.DS_Store`) — [macOS.gitignore](https://github.com/github/gitignore/blob/main/Global/macOS.gitignore)

Git only ignores **untracked** files. Already-committed `bin/` / `obj/` stay tracked until removed from the index ([git-scm gitignore](https://git-scm.com/docs/gitignore)).

- [ ] Expand `.gitignore` for a Visual Studio / .NET project (`[Bb]in/`, `[Oo]bj/`, `.vs/`, `*.user`, NuGet caches, etc.)
- [ ] Keep existing Rhino rules (`*.yak`, `*.bak`, `*.3dmbak`, `*.dmp`)
- [ ] Change Mac rule from `*.DS_Store` to `.DS_Store`
- [ ] Untrack build output **without deleting local files**:

```powershell
git rm -r --cached bin/ obj/
git rm --cached Documentation/Examples/.DS_Store Documentation/Reproduce/.DS_Store
```

- [ ] Confirm `bin\Debug\net48\SpruceBeetle.gha` still exists on disk (Grasshopper developer path)
- [ ] Commit: gitignore + stop tracking build artifacts
- [ ] After the commit, a Debug rebuild should **not** show `bin/` or `obj/` in `git status`

---



## Phase 2 — One version number; Yak is a package, not source

McNeel builds Yak from a staging folder (`.gha` + `manifest.yml` + icon + `misc/`), not from `bin/` ([Creating a Grasshopper Plug-In Package](https://developer.rhino3d.com/guides/yak/creating-a-grasshopper-plugin-package/)). Yak can read the assembly version with `version: $version`.

Today these disagree:


| Place                                    | Current                          |
| ---------------------------------------- | -------------------------------- |
| `SpruceBeetle.csproj` `<Version>`        | `0.1`                            |
| `Compiled/SpruceBeetle/manifest.yml`     | `1.0.1`                          |
| `SpruceBeetleInfo.cs`                    | no version override              |
| `Compiled/SpruceBeetle/SpruceBeetle.gha` | different size than `bin\` build |


- [ ] Pick **one** version (suggest keep `1.0.1` until you ship a real change, then bump)
- [ ] Set it in `SpruceBeetle.csproj` (`Version` / `InformationalVersion`)
- [ ] Make `GH_AssemblyInfo` report that assembly version (so Grasshopper and Yak match)
- [ ] Set Yak `manifest.yml` to `version: $version` (or the same number, not a third one)
- [ ] Decide the role of `Compiled/`:
  - **Keep:** `manifest.yml`, `icon.png`, `misc/LICENSE`, `misc/README.md`
  - **Do not treat as source of truth:** `SpruceBeetle.gha`, `CromulentBisgetti.ContainerPacking.dll` (copy in at `yak build` time, or publish via GitHub Releases / Package Manager)
- [ ] Leave `*.yak` ignored (already correct)

Skip publishing a Yak package until you actually want to ship. Setup guide Step 0 already says this.

---



## Phase 3 — Dead files (when you next touch the project file)

Safe once Phase 1 is done. Confirm no example `.gh` still expects a Custom Joints component (the class is fully commented out, so Grasshopper is not registering it now).

### Code

- [ ] Delete or archive `Alignment/CustomJoints.cs` (entire file is commented out)
- [ ] Remove the empty “Solution Items” folder from `SpruceBeetle.sln`



### Unused icons

Embedded or on disk, unused by any live component:

- [ ] `Resources/24x24_BinPacking.png` (orphan file)
- [ ] `Resources/24x24_BinPackingPy.png` (in `.resx`; Python packing component is gone)
- [ ] `Resources/24x24_ContainerPacking.png` (orphan file)
- [ ] `Resources/24x24_DirectAlignment.png` (in `.resx`; no component uses it)

Then drop matching entries from `Properties/Resources.resx` and the `EmbeddedResource` / `None Remove` lists in `SpruceBeetle.csproj`.

### csproj leftover `None Remove` (files that do not exist)

- [ ] `24x24_BinPacking_EBAFIT.png`
- [ ] `24x24_CreateJoints.png`
- [ ] `24x24_CustomJoints.png`
- [ ] `24x24_DeconstructOffcut.png` (the real file is `24x24_Deconstruct Offcut.png` with a space)
- [ ] `24x24_ExcelToOffcut.png`
- [ ] `24x24_FindConnections.png`
- [ ] `24x24_LapJoints.png`
- [ ] `24x24_UpcycleTimber.png`
- [ ] `Resources\Alignment.png`
- [ ] `Resources\AlignmentOptimized.png`
- [ ] `Resources\CromulentBisgetti.ContainerPacking.dll`
- [ ] `Resources\UpcycleTimber.png`

Optional, not required:

- [ ] Rename `24x24_Deconstruct Offcut.png` to drop the space (update `.resx` / csproj in the same change)

---



## Phase 4 — This copy is yours (docs / identity)

Do this when you want the public repo to describe **this** project, not Dominik’s original. MIT still requires keeping his copyright.

- [ ] Add your copyright line **in addition to** his in `LICENSE` (do not replace his)
- [ ] README: badges, issues, license, and install links → `Cfree1989/Spruce-Beetle-2.0`
- [ ] README: short credit + link to [DominikReisach/Spruce-Beetle](https://github.com/DominikReisach/Spruce-Beetle)
- [ ] Drop or rewrite the “Give up GitHub” section (his campaign, not a license requirement)
- [ ] `Compiled/SpruceBeetle/manifest.yml` `url:` → your repo
- [ ] `Compiled/SpruceBeetle/misc/README.md` GitHub link → your repo + original credit
- [ ] README Mac claim: honest about **Windows + Excel COM**. CSV/JSON work without Office; Excel needs Excel on Windows
- [ ] Plugin `AuthorName` / Yak `authors`: keep Dominik until you publish under your name; then list both
- [ ] Keep `Setup/` as internal notes, or say in README that it is for maintainers

---



## Later / optional (not cleanup)

- [ ] Isolate or replace Excel COM (`ClosedXML` / `ExcelDataReader`) so the plugin builds without Office
- [ ] Multi-target `net48;net7.0` for Rhino 8 default runtime (Setup guide Step 0 — skip until the tab is boring)
- [ ] Git LFS for huge docs (`sprucebeetle_examples.3dm` ~89 MB, `offcut_tales.obj` ~88 MB) if clones hurt
- [ ] Work PC: Office missing may fail the Excel COM build (see setup guide blockers)

---



## Suggested commits

Keep commits small so review is easy:

1. `Stop tracking bin/obj and ignore Visual Studio build output`
2. `Align plugin, csproj, and Yak version`
3. `Remove unused CustomJoints and leftover icons`
4. `Point README and Yak metadata at this repo; keep original MIT credit`

Do not commit `.env`, secrets, or a rebuilt `.gha` as a substitute for Phase 1.