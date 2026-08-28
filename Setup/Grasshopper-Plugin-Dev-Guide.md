# Spruce Beetle — Grasshopper Plugin Setup Guide

Work through this **in order**. Do not start changing component logic until Step 8 succeeds (the plugin tab appears in Grasshopper).

**Home PC (2026-08-27):** Steps 1–8 are done. The plugin tab loads. Skip file-edit steps (3–5) on a new machine; they are already in git.

**Work PC:** see [Second computer](#second-computer--work-pc) — repeat only the per-machine steps.

This is a C# Grasshopper plugin (`.gha`) for Rhino. You do **not** create a new project from a template. This repo already *is* that project. The job is to get a build-and-load loop working on this machine, then edit components.

Official McNeel references:

- [Your First Component (Windows)](https://developer.rhino3d.com/guides/grasshopper/your-first-component-windows/)
- [Installing Tools (Windows)](https://developer.rhino3d.com/guides/rhinocommon/installing-tools-windows)
- [Creating a Grasshopper Plug-In Package](https://developer.rhino3d.com/guides/yak/creating-a-grasshopper-plugin-package/)
- [Rhino 8 .NET Core vs .NET Framework](https://www.wiki.mcneel.com/zoo/rhinonetcore)

---

## How Grasshopper plugins work

A Grasshopper plugin is a C# class library that compiles to a **`.gha` file** (a renamed `.dll`). Grasshopper loads that file at startup and registers every class that inherits `GH_Component`.

The development loop:

1. Edit C# (Cursor or Visual Studio).
2. Build → `SpruceBeetle.gha`.
3. Restart Rhino / Grasshopper (or F5-debug into Rhino).
4. Place the component on the canvas and test.

Grasshopper **locks** the `.gha` while Rhino is open. After every rebuild you must fully close Rhino, or the old plugin stays loaded.

---

## What this repo already is

| Item | Value |
|---|---|
| Language | C# |
| Solution | `SpruceBeetle.sln` |
| Project | `SpruceBeetle.csproj` |
| Target | `.NET Framework 4.8` (`net48`) |
| Output | `SpruceBeetle.gha` |
| Grasshopper SDK | NuGet package `Grasshopper` 7.15.22039.13001 |
| Intended host | Rhino 7 or 8 |
| Plugin identity | `SpruceBeetleInfo.cs` (`GH_AssemblyInfo`) |
| Components | One `*_GH.cs` file per component |

Each component follows McNeel’s pattern:

- constructor (`base("Name", "Nickname", "Description", "Category", "Subcategory")`)
- `RegisterInputParams`
- `RegisterOutputParams`
- `SolveInstance` (the actual work)

Folders:

- `Base/` — data types (`Offcut`, joints, utilities)
- `Create/` — build offcut instances from numbers, CSV, Excel, JSON
- `Alignment/` — place offcuts along curves, joints
- `Packing/` — 3D bin packing
- `Fabricate/` — fabrication data / coordinates
- `Documentation/Examples/` — example `.gh` / `.3dm` files for testing

**Do not change component GUIDs.** Existing `.gh` files bind to those IDs.

---

## Git / GitHub — this copy is already yours

Checked 2026-08-27. **`git` is not pushing to the original author’s GitHub.**

| Check | Result |
|---|---|
| Local remote `origin` | `https://github.com/Cfree1989/Spruce-Beetle-2.0` |
| Other remotes (`upstream`, etc.) | None |
| GitHub fork relationship | **Not a fork** (`fork: false` on the GitHub API) |
| Push target | Your repo only. A `git push` cannot update Dominik Reisach’s repo. |

What still *looks* connected, and is not a git remote:

1. **README / Yak metadata still link to his GitHub.** Badges, release links, and `Compiled/SpruceBeetle/manifest.yml` (`url: https://github.com/dominikreisach/Spruce-Beetle`) are leftover text from the original project. They do not give him write access to your repo.
2. **The Git Graph is mostly his old timeline.** Copying a repo copies **every past commit**. Newer commits on `main` are yours (Rhino 8 paths, packing NuGet, this guide). The cloud icon on `main` tracks **your** `origin/main` (`Cfree1989/Spruce-Beetle-2.0`), not his.
3. **The merge commit that names his GitHub URL is from 2023.** `Merge branch 'main' of https://github.com/dominikreisach/Spruce-Beetle` is a snapshot of *him* merging on his machine years ago. It is frozen history. It is not a current remote.
4. **MIT license still requires keeping his copyright** in source files (`Copyright (c) 2022 Dominik Reisach`). That is attribution, not a GitHub connection. Do not strip it.

Newer commits appear **above** his history with your name. That is the normal way to take over a copied MIT project. Do not rewrite or delete his commits unless you explicitly decide you want a blank-history repo (that is a separate, destructive step).

To confirm anytime:

```powershell
git remote -v
```

You should only see `Cfree1989/Spruce-Beetle-2.0`. If a second remote named `upstream` pointing at `DominikReisach/Spruce-Beetle` ever appears, remove it with:

```powershell
git remote remove upstream
```

Do **not** run `git remote remove origin` unless you intend to disconnect your own GitHub repo too.

Optional later cleanup (not required for setup): rewrite README / `manifest.yml` so install links point at *your* repo, and keep a short credit to the original project. That is a docs change, not a git-remote change.

---

## Machine snapshot — home PC (checked 2026-08-27)

This table is **this computer**, not the work PC. Update it if this machine changes.

| Check | Status when documented | Notes |
|---|---|---|
| Rhino 8 | Installed | `C:\Program Files\Rhino 8\System\Rhino.exe` |
| Rhino 7 | Not installed | Debug/launch now point at Rhino 8 (Step 3) |
| Yak CLI | Present | `C:\Program Files\Rhino 8\System\Yak.exe` |
| Visual Studio 2022 | Installed 2026-08-27 | Community 17.14.39 at `C:\Program Files\Microsoft Visual Studio\2022\Community` |
| .NET SDK | Installed | 9.0.317 (came with Visual Studio) |
| .NET Framework 4.8 targeting pack | Installed | `v4.8` reference assemblies present |
| Microsoft Office | Present | Needed for the Excel COM reference |
| Grasshopper Libraries folder | Exists | Confirmed after Grasshopper first launch |
| `CromulentBisgetti.ContainerPacking.dll` | Restored via NuGet 1.0.0 | Step 5 |
| First Debug build | Succeeded 2026-08-27 | `bin\Debug\net48\SpruceBeetle.gha` (use VS MSBuild, not `dotnet build`) |
| Plugin loads in Grasshopper | Confirmed | Spruce Beetle tab visible (Step 8) |

---

## Second computer / work PC

The project fixes **travel with git**. Visual Studio, Rhino, and Grasshopper developer settings **do not**. Do not copy `bin\` from the home PC; rebuild on the work PC.

Clone **your** repo, not the original author’s:

`https://github.com/Cfree1989/Spruce-Beetle-2.0`

### Repeat (per machine)

1. **Step 1** — Visual Studio 2022 Community, .NET desktop workload, .NET Framework 4.8 targeting pack (needs admin).
2. **Step 2** — Rhino 8 licensed and installed; open Grasshopper once so Libraries exists.
3. Clone the repo (path can differ, e.g. not `C:\Repos\Spruce-Beetle-2.0`).
4. **Step 6** — Build with **Visual Studio MSBuild**, not `dotnet build` (Excel COM / MSB4803).
5. **Step 7** — `_GrasshopperDeveloperSettings` → add **this PC’s** `bin\Debug\net48` folder (whatever the clone path is) → uncheck Memory load.
6. **Step 8** — Fully quit Rhino, reopen, confirm the Spruce Beetle tab.

### Skip (already in the repo)

- Step 3 (Rhino 8 `/netfx` debug paths)
- Step 4 (Dominik post-build removed)
- Step 5 (packing package is a NuGet reference)

If Rhino 8 is not at `C:\Program Files\Rhino 8\System\Rhino.exe`, update `SpruceBeetle.csproj` and `Properties/launchSettings.json` on that machine only.

### Likely blockers at work

| Blocker | What it means |
|---|---|
| No admin rights | Cannot install Visual Studio / 4.8 targeting pack |
| No Rhino 8 license | Cannot load or debug the `.gha` |
| GitHub blocked | Copy the repo on USB (include `.git`), or use whatever git host work allows |
| No Microsoft Office | Build may fail on the Excel COM reference; CSV/JSON components can still be used if we drop or isolate Excel later |
| NuGet blocked | Restore will fail until packages can download |

USB fallback: copy the whole repo folder (including `.git`). Still install VS + Rhino on that PC, then start at Step 1.

---

## What is currently broken in this repo

File-edit items below are **already fixed in git**. Revisit them only if a new machine still has an old clone.

1. **Debug path pointed at Rhino 7 — fixed (Step 3)**
   - `SpruceBeetle.csproj` and `Properties/launchSettings.json` now start `C:\Program Files\Rhino 8\System\Rhino.exe` with `/netfx`.

2. **Post-build copy used the author’s user folder — fixed (Step 4)**
   - Removed the `PostBuild` target that copied to `C:\Users\Dominik\...` and deleted the `.gha`. Grasshopper will load from `bin\Debug\net48` in Step 7.

3. **Missing packing DLL — fixed (Step 5)**
   - Replaced the `bin\Debug\net48\` HintPath with NuGet package `CromulentBisgetti.ContainerPacking` 1.0.0. `dotnet restore` succeeded.

4. **Rhino 8 default runtime is .NET Core, this plugin is net48**
   - Many net48 plugins still load.
   - Excel COM interop and some Framework-only APIs can fail under the default runtime.
   - Fallback: Rhino command `_SetDotNetRuntime` → .NET Framework, then restart, or launch with `/netfx`.

---

## Step 0 — Do not start here

Skip these until the plugin loads:

- [ ] Rewriting the project as Rhino 8 multi-target (`net48;net7.0`)
- [ ] Changing component GUIDs
- [ ] Editing `SolveInstance` / adding new components
- [ ] Publishing a Yak package

---

## Step 1 — Install Visual Studio 2022 and the 4.8 targeting pack

Visual Studio Community is enough. Cursor can edit the code; VS 2022 is the reliable way to F5-debug into Rhino.

1. Download [Visual Studio 2022 Community](https://visualstudio.microsoft.com/vs/community/).
2. Run the installer.
3. Enable workload: **.NET desktop development**.
4. Individual components — check:
   - **.NET Framework 4.8 SDK**
   - **.NET Framework 4.8 targeting pack**
5. Install.

If VS is already installed, re-run **Visual Studio Installer** and add those components.

**Done when:**

- Visual Studio 2022 opens.
- You can create a dummy .NET Framework 4.8 class library (you do not need to keep it).

Optional McNeel templates (not required for *this* repo, useful later):

```powershell
dotnet new install Rhino.Templates
```

Or the [Rhino Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=McNeel.Rhino7Templates2022).

---

## Step 2 — Confirm Rhino 8 and Grasshopper

1. Launch **Rhino 8**.
2. Type `Grasshopper` and press Enter.
3. Confirm the Grasshopper window opens.
4. In Rhino, type `_GrasshopperFolders` or check that this folder now exists:

   `%APPDATA%\Grasshopper\Libraries\`

   Full typical path:

   `C:\Users\<You>\AppData\Roaming\Grasshopper\Libraries\`

**Done when:** Grasshopper opens and the Libraries folder exists.

---

## Step 3 — Point debug / launch at Rhino 8

Edit these two files. Do not change anything else yet.

### `SpruceBeetle.csproj`

Find:

```xml
<StartProgram>C:\Program Files\Rhino 7\System\Rhino.exe</StartProgram>
```

Replace with:

```xml
<StartProgram>C:\Program Files\Rhino 8\System\Rhino.exe</StartProgram>
```

Optional (helps if net48 plugins fail to load under Rhino 8’s default .NET Core runtime):

```xml
<StartArguments>/netfx</StartArguments>
```

### `Properties/launchSettings.json`

Replace the executable path with Rhino 8:

```json
{
  "profiles": {
    "SpruceBeetle": {
      "commandName": "Executable",
      "executablePath": "C:\\Program Files\\Rhino 8\\System\\Rhino.exe",
      "commandLineArgs": "/netfx"
    }
  }
}
```

**Done when:** both files point at `C:\Program Files\Rhino 8\System\Rhino.exe`.

---

## Step 4 — Fix the post-build copy

The current post-build in `SpruceBeetle.csproj`:

```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <Exec Command="Copy &quot;$(TargetPath)&quot; &quot;C:\Users\Dominik\AppData\Roaming\Grasshopper\Libraries\SpruceBeetle\SpruceBeetle.gha&quot;&#xD;&#xA;Erase &quot;$(TargetPath)&quot;" />
</Target>
```

Two problems: it copies to Dominik’s folder, and it **deletes** the build output (`Erase`), which makes debugging harder.

**Recommended for development:** remove this `PostBuild` target entirely. Load the `.gha` from the build folder instead (Step 7).

If you prefer auto-install into Grasshopper Libraries, replace it with a path that uses your user profile, and **do not erase** the build output:

```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <MakeDir Directories="$(AppData)\Grasshopper\Libraries\SpruceBeetle" />
  <Copy SourceFiles="$(TargetPath)" DestinationFiles="$(AppData)\Grasshopper\Libraries\SpruceBeetle\SpruceBeetle.gha" />
</Target>
```

Also copy `CromulentBisgetti.ContainerPacking.dll` next to the `.gha` if you use this install method. Grasshopper will not find a dependency that only lives in `bin\`.

**Done when:** the Dominik path is gone, and a Debug build no longer fails on Copy/Erase.

---

## Step 5 — Get the packing DLL

The Packing component will not compile without `CromulentBisgetti.ContainerPacking.dll`.

**Option A — GitHub release (matches original install docs)**

1. Open [Spruce Beetle releases](https://github.com/dominikreisach/Spruce-Beetle/releases/).
2. Download `SpruceBeetle.zip`.
3. Extract `CromulentBisgetti.ContainerPacking.dll`.
4. Put it where the `.csproj` expects it:

   `bin\Debug\net48\CromulentBisgetti.ContainerPacking.dll`

   Create those folders if they do not exist.

**Option B — NuGet (cleaner for development)**

Add a package reference instead of a HintPath:

```xml
<PackageReference Include="CromulentBisgetti.ContainerPacking" Version="1.0.0" />
```

Then remove the `HintPath` `<Reference>` for that DLL.

After either option, keep a copy of the DLL **next to the `.gha`** when you install into Libraries, because Grasshopper loads it at runtime.

**Done when:** the packing assembly exists on disk (or as a restored NuGet package) so the compiler can resolve `CromulentBisgetti.ContainerPacking`.

---

## Step 6 — First build

1. Close Rhino if it is open (it can lock the `.gha`).
2. Open `SpruceBeetle.sln` in Visual Studio 2022 and Build, **or** use Visual Studio’s MSBuild (not `dotnet build`):

   ```powershell
   & "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" C:\Repos\Spruce-Beetle-2.0\SpruceBeetle.csproj /p:Configuration=Debug /restore
   ```

   `dotnet build` fails with **MSB4803** because the Excel COM reference (`Microsoft.Office.Interop.Excel`) is not supported on the .NET Core MSBuild that `dotnet` uses. Visual Studio’s MSBuild is the .NET Framework one and can resolve COM.

3. Confirm this file exists:

   `bin\Debug\net48\SpruceBeetle.gha`

   First successful Debug build (2026-08-27) also copied `CromulentBisgetti.ContainerPacking.dll` and `Newtonsoft.Json.dll` next to the `.gha`. Two existing warnings in `DeconstructOffcut_GH.cs` (`CS0472`) are leftover and do not fail the build.

Likely first-build failures and what they mean:

| Error | Cause | Fix |
|---|---|---|
| No .NET SDKs were found | Step 1 not done | Install VS / SDK |
| Targeting pack net48 not found | 4.8 targeting pack missing | VS Installer → 4.8 targeting pack |
| Cannot find CromulentBisgetti… | Step 5 not done | Add the DLL or NuGet package |
| Copy failed … `\Users\Dominik\...` | Step 4 not done | Fix/remove PostBuild |
| MSB4803 ResolveComReference | Used `dotnet build` | Use VS MSBuild / Build in Visual Studio |
| Excel / COM / Interop error | Excel COM reference | Confirm Office is installed; only the Excel component needs it |
| File locked / cannot copy `.gha` | Rhino is still open | Close Rhino completely |

**Done when:** a Debug build succeeds and `SpruceBeetle.gha` is in `bin\Debug\net48\`. **Completed.**

---

## Step 7 — Tell Grasshopper where to load the plugin

In Rhino 8:

1. Run command `_GrasshopperDeveloperSettings`.
2. Add the folder:

   `C:\Repos\Spruce-Beetle-2.0\bin\Debug\net48`

3. **Uncheck** “Memory load *.GHA assemblies” (required for debugging / breakpoints).
4. OK.
5. If you previously installed the release `.gha` into Libraries, remove or rename that copy so you do not load **two** versions with the same GUID.

Windows sometimes blocks downloaded `.gha` / `.dll` files. If Grasshopper refuses to load:

1. Right-click the file → Properties.
2. If you see **Unblock**, check it → Apply.

**Done when:** Developer Settings lists the `bin\Debug\net48` folder and Memory load is off. **Completed (user confirmed).**

---

## Step 8 — Confirm it loads (gate before any feature work)

1. Fully close Rhino, then reopen Rhino 8.
2. Run `Grasshopper`.
3. Look for a **Spruce Beetle** tab (category icon is set in `SpruceBeetleInfo.cs`).
4. Drop a simple component such as **Construct Offcut** onto the canvas.
5. If the tab is missing:
   - Grasshopper menu → **File → Special Folders → Components Folder** and confirm you are not also loading an old copy.
   - In Rhino, run `_SetDotNetRuntime`, choose **.NET Framework**, restart Rhino, try again.
   - Or launch Rhino with:

     `"C:\Program Files\Rhino 8\System\Rhino.exe" /netfx`

6. Optional smoke test: open files under `Documentation/Examples`.

**Done when:** the Spruce Beetle tab is visible and Construct Offcut can be placed on the canvas. **Completed (user confirmed).**

Plugin logic can be edited from here.

---

## Step 9 — Debug loop (F5)

Once Step 8 works:

1. Open `SpruceBeetle.sln` in Visual Studio 2022.
2. Set a breakpoint on the first line of `SolveInstance` in `Create/ConstructOffcut_GH.cs`.
3. Press **F5**. Rhino 8 should launch (from Step 3 paths).
4. In Rhino, run `Grasshopper`.
5. Place **Construct Offcut**, connect inputs, and trigger a solve. The breakpoint should hit.

If F5 starts the wrong Rhino, re-check Step 3.

If breakpoints are hollow / never hit:

- Memory load is still on (Step 7).
- Grasshopper loaded a different `.gha` from Libraries.
- You are debugging Release instead of Debug.

Day-to-day loop after this:

1. Edit code.
2. Stop debugging / close Rhino.
3. Rebuild.
4. F5 or reopen Rhino + Grasshopper.
5. Test the changed component.

---

## Step 10 — Then we can modify the plugin

Only after Step 8:

- Change existing component behavior in the matching `*_GH.cs` file (`SolveInstance` and params).
- Shared types live in `Base/`.
- New component = new class inheriting `GH_Component`, new GUID, icon in `Resources/`, entry in `Properties/Resources.resx` if you follow the existing pattern.
- Keep the category name `"Spruce Beetle"` unless you intend to move the tab.

When you are ready to distribute (later, not now):

1. Build Release.
2. Put `SpruceBeetle.gha`, `CromulentBisgetti.ContainerPacking.dll`, `manifest.yml`, and `icon.png` in one folder.
3. From that folder:

   ```powershell
   & "C:\Program Files\Rhino 8\System\Yak.exe" spec
   & "C:\Program Files\Rhino 8\System\Yak.exe" build
   ```

   Existing Yak metadata lives in `Compiled/SpruceBeetle/manifest.yml`.

---

## Quick reference — files you will touch for setup vs features

| File | Setup | Feature work |
|---|---|---|
| `SpruceBeetle.csproj` | Yes (Rhino path, PostBuild, packing ref) | Rarely |
| `Properties/launchSettings.json` | Yes | No |
| `bin\Debug\net48\CromulentBisgetti.ContainerPacking.dll` | Yes | No |
| `SpruceBeetleInfo.cs` | No | Only if renaming the plugin |
| `Create/*_GH.cs` etc. | No | Yes |
| `Base/*.cs` | No | Yes, if data types change |

---

## If you get stuck

1. Close Rhino completely, rebuild, reopen.
2. Confirm only **one** `SpruceBeetle.gha` is on Grasshopper’s search path.
3. Try `/netfx` or `_SetDotNetRuntime`.
4. Unblock `.gha` / `.dll` in file Properties.
5. Check the Grasshopper load errors (hover the plugin icon / look at the GH load dialog).
