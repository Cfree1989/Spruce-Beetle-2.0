<!-- Header -->
<div align="center">

  <img src="Resources/imgs/SpruceBeetleIcon.png" alt="logo" width="200" height="auto" />
  <h1>Spruce Beetle 2.0</h1>

  <p>
    A thesis working copy of the toolkit for designing with timber offcuts in <a href="https://www.rhino3d.com">Rhino/Grasshopper</a>.
  </p>

<p>
  <a href="https://github.com/Cfree1989/Spruce-Beetle-2.0/commits/main">
    <img src="https://img.shields.io/github/last-commit/Cfree1989/Spruce-Beetle-2.0" alt="last update" />
  </a>
  <a href="https://github.com/Cfree1989/Spruce-Beetle-2.0/issues/">
    <img src="https://img.shields.io/github/issues/Cfree1989/Spruce-Beetle-2.0" alt="open issues" />
  </a>
  <a href="https://github.com/Cfree1989/Spruce-Beetle-2.0/blob/master/LICENSE">
    <img src="https://img.shields.io/github/license/Cfree1989/Spruce-Beetle-2.0.svg" alt="license" />
  </a>
</p>
</div>

<br />

# Contents

- [About](#about)
  - [Installation](#installation)
  - [Grasshopper Components](#grasshopper-components)
  - [Documentation](#documentation)
  - [Examples](#examples)
  - [Reproduce *Offcut Tales*](#reproduce-the-offcut-tales-demonstrator)
- [Acknowledgements](#acknowledgements)
- [Citing](#citing)
  - [Conference Paper](#conference-paper)
  - [Plugin](#plugin)

# About

*Spruce Beetle 2.0* is a working copy of Dominik Reisach’s [Spruce Beetle](https://github.com/DominikReisach/Spruce-Beetle) plugin, used for master’s thesis development. The original toolkit helps reuse timber offcuts in Rhino/Grasshopper: dimensional stock → curve-based alignment or 3D packing → dry joints → fabrication data.

This fork keeps the original MIT license and copyright. Plugin `AuthorName` remains Dominik Reisach until a named release of this copy.

## Installation

You need [Rhino](https://www.rhino3d.com) 7 or 8 with a valid license.

**CSV to Offcut** and **JSON to Offcut** work on Windows and Mac. **Excel to Offcut** needs Microsoft Excel on Windows (COM interop).

To install this fork:

1. Build from source (see [Setup/Grasshopper-Plugin-Dev-Guide.md](Setup/Grasshopper-Plugin-Dev-Guide.md)), then place `SpruceBeetle.gha` and `CromulentBisgetti.ContainerPacking.dll` in your Grasshopper Components folder, **or**
2. Copy the snapshot in [`Compiled/SpruceBeetle/`](Compiled/SpruceBeetle/) into that folder. That snapshot is a last-known-good load path, not the source of truth — rebuild for the latest components.

The original plugin is also available from [upstream releases](https://github.com/DominikReisach/Spruce-Beetle/releases/) and Rhino’s Package Manager. Those packages are **not** this thesis fork.

## Grasshopper Components

<div align="center">
  <img src="Resources/imgs/sb_components.png" alt="Spruce Beetle Grasshopper components" />
</div>

## Documentation

Thesis and maintainer docs for this copy:

- [Documentation/README.md](Documentation/README.md) — index
- [Component-Reference.md](Documentation/Component-Reference.md) — component catalog (packing tables still stale)
- [Packing-Joints.md](Documentation/Packing-Joints.md) — packed-column joint spec
- [Column-Fill-2x2x8.md](Documentation/Column-Fill-2x2x8.md) — 24″ × 24″ × 96″ column fill
- [Thesis-Change-Log.md](Documentation/Thesis-Change-Log.md) — lab notebook
- [Setup/Grasshopper-Plugin-Dev-Guide.md](Setup/Grasshopper-Plugin-Dev-Guide.md) — build and load loop

Stock CSVs/JSON for packing tests: [Documentation/TestData/](Documentation/TestData/).

## Examples

Original screenshots and files from [Documentation/Examples](Documentation/Examples), i.e. `sprucebeetle_examples.3dm` and `sprucebeetle_examples.gh`.

### Create Offcut Instances

<div align="center">
  <img src="Documentation/Examples/sprucebeetle_example_offcuts.png" alt="Grasshopper canvas showing how to create offcut instances from data." />
</div>

### Exemplary Workflow Alignment

<div align="center">
  <img src="Documentation/Examples/sprucebeetle_example_workflow.png" alt="Grasshopper canvas showing an example alignment workflow." />
</div>

### Exemplary Workflow Bin-Packing

<div align="center">
  <img src="Documentation/Examples/sprucebeetle_example_packing.png" alt="Grasshopper canvas showing an example workflow for 3D bin-packing." />
</div>

### Example Designs

<div align="center">
  <img src="Documentation/Examples/sprucebeetle_examples_1.png" alt="Example designs." />
</div>

<div align="center">
  <img src="Documentation/Examples/sprucebeetle_examples_2.png" alt="Example designs." />
</div>

## Reproduce the *Offcut Tales* Demonstrator

<div align="center">
  <img src="Documentation/Reproduce/offcut_tales_photo.jpg" alt="A photograph of the Offcut Tales demonstrator." />
</div>

<div align="center">
  <img src="Documentation/Reproduce/offcut_tales_design.jpg" alt="Drawings of the Offcut Tales demonstrator." />
</div>

All files to reproduce the *Offcut Tales* demonstrator are under [Documentation/Reproduce](Documentation/Reproduce):

1. A Rhino file with the geometric data: `offcut_tales.3dm`
2. A platform-independent file containing the geometric data: `offcut_tales.obj`
3. A Grasshopper file with the script to produce the demonstrator: `offcut_tales.gh`
4. The numerical data of all offcuts that were available in this research project: `offcuts.csv`

In `offcut_tales.3dm` and `offcut_tales.obj` you will find the geometric data of the demonstrator as fabricated. If you use `offcut_tales.gh`, provide the correct path to `offcuts.csv` in the *Panel* at the start of the script:

<div align="center">
  <img src="Documentation/Reproduce/offcut_tales.png" alt="Screenshot of a Grasshopper canvas" />
</div>

# Acknowledgements

The original toolbox was developed at the [Bauhaus-Universität Weimar](https://www.uni-weimar.de/en), Germany, in the scope of the master’s thesis
<br><i>Upcycle Timber: A Design-to-Fabrication Workflow for Free-Form Timber Structures with Offcuts</i>.

That thesis was supervised by [Professor Dr. Jan Willmann](https://www.uni-weimar.de/en/art-and-design/chairs/theory-and-history-of-design/), [Dr. Sven Schneider](https://www.uni-weimar.de/de/architektur-und-urbanistik/professuren/infar), and [Dr. Stephan Schütz](https://www.strukturstudio.de/).

# Citing

## [Conference Paper](https://doi.org/10.1007/978-3-031-37189-9_24)

```
@incollection{reisachDesigntoFabricationWorkflowFreeForm2023,
  title = {A {Design-to-Fabrication Workflow} for {Free-Form Timber Structures Using Offcuts}},
  booktitle = {Computer-{Aided Architectural Design}. {INTERCONNECTIONS}: {Co-computing Beyond Boundaries}. {CAAD Futures 2023}},
  author = {Reisach, Dominik and Sch{\"u}tz, Stephan and Willmann, Jan and Schneider, Sven},
  editor = {Turrin, Michela and Andriotis, Charalampos and Rafiee, Azarakhsh},
  year = {2023},
  series = {Communications in {Computer and Information Science}},
  volume = {1819},
  pages = {361--375},
  publisher = {Springer},
  address = {Cham},
  doi = {10.1007/978-3-031-37189-9_24},
  isbn = {978-3-031-37189-9},
}
```

## [Plugin](https://doi.org/10.5281/zenodo.10071468)

```
@misc{spruce_beetle,
    title={Spruce Beetle},
    author={Dominik Reisach},
    year={2023},
    doi={10.5281/zenodo.10071468},
    url={https://github.com/DominikReisach/Spruce-Beetle}
}
```
