# Spruce Beetle — Component Reference

Spruce Beetle is a Grasshopper toolkit for designing with timber (or other) **offcuts**: rectangular leftover pieces described by an index and three dimensions. Typical workflow:

1. **Create** offcut records from numbers, CSV, Excel, or JSON.
2. **Align** them along a curve (straight or free-form).
3. Optionally **unify** the stack, then cut **joints**.
4. **Pack** leftovers into a container, or **orient** pieces for fabrication and export data.

Grasshopper category: **Spruce Beetle**. Subcategories: Create, Alignment, Packing, Fabricate.

Dimensions are whatever units your Rhino document uses. Examples in this repo (for example `offcuts.csv`) are in metres: `index;x;y;z`.

---

## The Offcut type

Most components pass a custom **Offcut** object (nickname `Oc` / `OcD` / `AOc`). It is more than a box: after alignment it also stores geometry and planes used for joints and fabrication.

| Field | Meaning |
| --- | --- |
| **Index** | ID of this piece in the stock list. Used to remove used pieces later. |
| **X, Y, Z** | Stock dimensions. **Z is the length along the alignment curve**; X and Y are the cross-section. |
| **Volume (`vol`)** | Stock volume (`X × Y × Z` when first constructed). |
| **Fabricated volume (`fvol`)** | Volume after cuts (alignment bevels, unification, joints). |
| **Brep** | Solid geometry of the piece in model space. Empty until alignment (or assemble). |
| **First / Second plane** | End planes of the piece (start and end along the curve). Joints are cut on these. |
| **Average plane** | Mid-length frame of the piece. |
| **Moved average plane** | Average plane shifted so its origin sits at the centre of the current cross-section (updated by Unification). |
| **Base plane** | Frame used to build the rectangular stock extrusion. |
| **Position index** | How the rectangle sits on the curve (see Offcut Position below). |

### Offcut Position (`OcP`)

Alignment components auto-create a value list. The string maps to how the X–Y rectangle is placed on the curve frame:

| Value | Index | Placement |
| --- | --- | --- |
| `mid-mid` | 0 | Centred on both axes (default) |
| `mid-top` | 1 | Centred in X, flush to +Y |
| `mid-bottom` | 2 | Centred in X, flush to −Y |
| `right-mid` | 3 | Flush to +X, centred in Y |
| `left-mid` | 4 | Flush to −X, centred in Y |
| `right-top` | 5 | +X, +Y corner |
| `right-bottom` | 6 | +X, −Y corner |
| `left-bottom` | 7 | −X, −Y corner |
| `left-top` | 8 | −X, +Y corner |

---

## Create

Build, split, and reassemble Offcut data.

### Construct Offcut (`ConOffcut`)

**What it does:** Builds Offcut objects from three parallel lists of dimensions. Indices are assigned as `0, 1, 2, …` in list order. Volume is `x × y × z`. No geometry yet.

**Inputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| x-Dimension | x | Number | List | Cross-section width of each piece. |
| y-Dimension | y | Number | List | Cross-section height of each piece. |
| z-Dimension | z | Number | List | Length of each piece (along the curve later). |

Lists must be the same length or the component errors.

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Offcut | Oc | Offcut | List | New stock records. |

---

### CSV to Offcut (`CSV 2 Oc`)

**What it does:** Reads a CSV and turns each row into an Offcut.

**Expected row format** (after splitting on the delimiter): `index`, `x`, `y`, `z`. Example from `Documentation/Reproduce/offcuts.csv`:

```text
1;0.246;0.083;0.286
```

**Inputs**

| Name | Nick | Type | Access | Default | Description |
| --- | --- | --- | --- | --- | --- |
| File Path | file | Text | Item | — | Full path to the CSV. |
| Delimiter | D | Text | Item | `;` | Character used to split each line. Only the first character is used. |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Offcut Data | OcD | Offcut | List | One Offcut per data row. |

---

### Excel to Offcut (`XLS 2 Oc`)

**What it does:** Opens the first worksheet of an Excel file via Excel Interop and reads dimensions. **Row 1 is treated as a header and skipped.** Columns: 1 = index, 2 = x, 3 = y, 4 = z.

Requires Excel on the machine (Windows).

**Inputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| File Path | file | Text | Item | Path to `.xls` / `.xlsx`. |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Offcut Data | OcD | Offcut | List | Offcuts from the used range. |

---

### Assemble Offcut (`Asmbl`)

**What it does:** Inverse of Deconstruct Offcut. Rebuilds complete Offcut objects when you already have geometry, planes, and volumes (for example after editing in Grasshopper).

All lists must have the same count.

**Inputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Index | i | Number | List | Stock index. |
| x-Dimension | x | Number | List | X size. |
| y-Dimension | y | Number | List | Y size. |
| z-Dimension | z | Number | List | Z size. |
| Volume | vol | Number | List | Stock volume. |
| Fabricated Volume | fvol | Number | List | Volume after fabrication. |
| Breps | B | Brep | List | Solid geometry. |
| First Plane | fp | Plane | List | Start end plane. |
| Second Plane | sp | Plane | List | Finish end plane. |
| Average Plane | ap | Plane | List | Mid plane. |
| Moved Average Plane | map | Plane | List | Shifted mid plane. |
| Base Plane | bp | Plane | List | Stock construction plane. |
| Position Index | pi | Integer | List | Offcut Position index (0–8). |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Offcut | Oc | Offcut | List | Reassembled Offcuts. |

---

### Deconstruct Offcut (`DeOffcut`)

**What it does:** Unpacks an Offcut into numbers, geometry, and planes so you can inspect or edit them. Plane outputs are hidden in the viewport by default. Fields that were never set (typical for stock-only Offcuts) are simply omitted from that list.

**Inputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Offcut | Oc | Offcut | List | One or more Offcuts. |

**Outputs** — same names as Assemble Offcut (Index through Position Index).

---

### Get Brep (`GetB`)

**What it does:** Extracts only the solid geometry from Offcuts. Warns if a piece has no Brep yet (not aligned).

**Inputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Offcuts | Oc | Offcut | List | Offcuts that already have geometry. |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Breps | B | Brep | List | `OffcutGeometry` of each piece. |

---

### List Update (`Update`)

**What it does:** Removes used Offcuts from a stock list by matching **Index**. Feed the original stock list and the pieces you already placed; you get leftovers.

This component is **hidden** in the Grasshopper toolbar (`GH_Exposure.hidden`) but still exists in the plugin.

**Inputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Offcut Data | OcD | Offcut | List | Full stock list. |
| Offcuts | Oc | Offcut | List | Pieces already used (aligned). |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Offcuts | Oc | Offcut | List | Stock minus used indices. |

---

### Offcut (parameter)

**What it does:** A Grasshopper parameter that holds a collection of Offcuts (like a Point or Curve param). It does not compute anything; it stores or receives Offcut data on the canvas.

---

## Alignment

Place Offcuts along a guide curve, then optionally trim and joint them.

Alignment walks from the **start of the curve toward the end**. While remaining chord-ish distance is greater than the longest remaining Z, it places the next piece. When the leftover span is shorter than the longest Z, it places **one last** piece (often the longest remaining) and stops. Leftovers come out as Unused Offcuts.

Linear curves use a simpler placement; non-linear curves tilt end planes using local curvature.

### Curve Alignment (`Align`)

**What it does:** Places Offcuts **in list order** (always takes the first remaining piece, except the last fill which uses the longest Z). Good when you want a fixed sequence.

**Inputs**

| Name | Nick | Type | Access | Default | Description |
| --- | --- | --- | --- | --- | --- |
| Curve | C | Curve | Item | — | Path to follow. Reparameterized to 0–1 internally. |
| Offcut Data | OcD | Offcut | List | — | Stock to consume. |
| Offcut Position | OcP | Text | Item | `mid-mid` | Cross-section placement (value list auto-added). |
| Start Angle | SA | Number | Item | `0` | Rotation of the start frame around the curve tangent, in **degrees**. |
| End Angle | EA | Number | Item | `0` | Rotation at the end. In-between pieces interpolate SA→EA along the curve. |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Aligned Offcuts | AOc | Offcut | List | Placed pieces with Breps and planes. Ends are typically bevelled to sit on the curve. |
| Unused Offcuts | UOc | Offcut | List | Stock that did not fit. Can feed another alignment. |
| Centroid Curve | CC | Curve | Item | Polyline through the centres of the placed pieces (hidden in the viewport by default). |

---

### Optimized Alignment (`OptiAlign`)

**What it does:** Same inputs and outputs as Curve Alignment, but **picks which Offcut to place next**. At each step it maps local curve curvature to the range of remaining Z lengths and chooses the piece whose Z is closest to that target (short pieces on tight bends, long pieces on flatter spans).

Use this when matching stock length to curvature matters more than list order.

---

### Test Alignment (`TestAlign`)

**What it does:** Dry-run of the **optimized** placement. It does not build Offcut solids; it only reports where joints/ends would land. Useful for checking if a curve can be covered before running the heavier alignment.

**Inputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Curve | C | Curve | Item | Guide curve. |
| Offcut Data | OcD | Offcut | List | Stock (consumed internally for the test; does not output unused pieces). |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Alignment Points | APt | Point | List | Origins of successive end frames, starting at the curve start. |
| Alignment Planes | AP | Plane | List | Those frames (hidden in the viewport by default). |

---

### Unification (`Unify`)

**What it does:** After alignment, pieces still have their original X×Y sizes, so the stack looks stepped. Unification finds the **smallest X and Y** in the list, sweeps a slightly smaller rectangle along the curve, and **boolean-intersects** every Offcut with that “min tube.” Result: a consistent cross-section envelope, with fabricated volumes updated.

**Inputs**

| Name | Nick | Type | Access | Default | Description |
| --- | --- | --- | --- | --- | --- |
| Curve | C | Curve | Item | — | Same (or similar) curve used for alignment; used as the sweep rail. |
| Aligned Offcuts | AOc | Offcut | List | — | Pieces with geometry and planes. |
| Scale | S | Number | Item | `0.975` | Uniform scale of the min rectangle about its centre. Slightly less than 1 avoids coincident faces that break booleans. |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Unified Offcuts | UOc | Offcut | List | Trimmed solids; `FabVol` and moved average planes updated. |

---

### Find Intersections (`Intersect`)

**What it does:** Pairwise curve–curve intersections for a set of alignment curves (centroid curves or design curves). Classifies each hit as **V**, **X**, or **T**.

Classification (per curve, using parameter `t`):

- **0 = V:** Intersection within `0.05` length units of a curve end (both curves).
- **1 = X:** Intersection away from both ends.
- **2 = T:** One curve hits the other near an end, the other in the middle (or mixed).

Trees are organized **per input curve**: branch `i` lists everything that curve connects to.

**Inputs**

| Name | Nick | Type | Access | Default | Description |
| --- | --- | --- | --- | --- | --- |
| Curves | C | Curve | List | — | Alignment / design curves. |
| Tolerance | T | Number | Item | `0.05` | Distance tolerance for `CurveCurve` intersection. |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Indices | I | Integer | Tree | Other curve index in the input list. |
| Intersection Type | IT | Integer | Tree | `0` V, `1` X, `2` T. |
| Parameter | t | Number | Tree | Parameter on **this** curve at the hit. |
| Curves | C | Curve | List | The same curves that were used (viewport-hidden). |
| Intersection Points | IntP | Point | Tree | Point on this curve at `t`. |

A remark reports how many intersections were found.

---

### Tenon Joints (`Tenon`)

**What it does:** Cuts tenon/mortise geometry on the **ends** of consecutive Offcuts in one alignment. First piece is cut on its second plane, last piece on its first plane, middle pieces on both. Joint size is limited using the overlapping neighbour’s smaller X/Y.

A value list is auto-added for joint type: `tenon`, `cross tenon`, `custom tenon`.

**Inputs**

| Name | Nick | Type | Access | Default | Description |
| --- | --- | --- | --- | --- | --- |
| Aligned Offcuts | AOc | Offcut | List | — | Ordered pieces along one curve. |
| Tool Radius | R | Number | Item | `0.005` | Corner fillet on the joint profile (milling bit). |
| Joint X | JX | Number | Item | `0.02` | Joint size in X. |
| Joint Y | JY | Number | Item | `0.05` | Joint size in Y. |
| Joint Z | JZ | Number | Item | `0.04` | Extrusion depth of the tenon (through the interface). |
| Joint Type | JT | Text | Item | — | `tenon` (rectangle), `cross tenon` (crossed rectangles), `custom tenon`. |
| Tenon Count | TC | Integer | Item | `1` | Number of tenons spaced along the interface. |
| Custom Shape | CS | Curve | Item | optional | Closed **planar** curve; scaled into JX×JY for `custom tenon`. |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Offcuts | Oc | Offcut | List | Solids after boolean difference; `FabVol` updated. |
| Joints | J | Brep | List | Cutter solids (viewport-hidden). |
| Joint Volume | JV | Number | List | Approximate volume of joints at each interface. |

---

### Spline Joints (`Spline`)

**What it does:** Same role as Tenon Joints, but cuts **dovetail-style spline slots** through the thickness (Y) instead of axial tenons. Use when you will insert a separate spline key.

**Inputs**

| Name | Nick | Type | Access | Default | Description |
| --- | --- | --- | --- | --- | --- |
| Aligned Offcuts | AOc | Offcut | List | — | Ordered aligned pieces. |
| Tool Radius | R | Number | Item | `0.005` | Fillet on the dovetail profile. |
| Joint X | JX | Number | Item | `0.02` | Spline width. |
| Joint Y | JY | Number | Item | `0.05` | Spline depth in the profile plane. |
| Spline Count | SC | Integer | Item | `1` | Number of splines along the interface. |

**Outputs** — same as Tenon Joints (`Oc`, `J`, `JV`). Display joints are shorter than the actual cutters so they read as keys sitting in the slot.

---

### Intersection Joints (`IntJoints`)

**What it does:** Where **two alignments cross**, cuts a joint into the Offcuts nearest the intersection point (the three closest pieces on each branch).

**Joint Type**

- `0` — spline-style: scales the first alignment’s closest solid and subtracts it from the second alignment (first alignment geometry is left unchanged).
- `1` — **cross-lap** (default in the description): overlapping half-laps on both alignments, using a shared average plane at the intersection.

**Inputs**

| Name | Nick | Type | Access | Default | Description |
| --- | --- | --- | --- | --- | --- |
| First Alignment | FA | Offcut | List | — | First chain of aligned Offcuts. |
| Second Alignment | SA | Offcut | List | — | Second chain. |
| Intersection Point | IP | Point | Item | — | Point from Find Intersections (`IntP`). |
| Rotate Joint | RJ | Number | Item | `0` | Rotation of the joint frame around the piece Z, in **degrees**. |
| Width | W | Number | Item | `1.0` | Scale factor for lap / cutter width. |
| Joint Type | JT | Integer | Item | `1` | `0` spline cut, `1` cross-lap. |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Offcuts | Oc | Offcut | Tree | Full alignments after cuts. Branch `{0}` = first, `{1}` = second. |
| Intersection Offcuts | IOc | Offcut | Tree | Only the pieces that were actually cut at the crossing. |

---

## Packing

### Bin Packing EB-AFIT (`PackBinC#`)

**What it does:** Packs Offcut **stock boxes** (X, Y, Z) into a 3D container using the EB-AFIT algorithm (full rotation of items). Builds closed Breps at packed locations in a box aligned to World XY from the origin, using the input box’s size.

Does not preserve Offcut objects — output is geometry only.

**Inputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Offcut Data | OcD | Offcut | List | Pieces to pack (`X`, `Y`, `Z`). |
| Box | B | Box | Item | Container size (X/Y/Z intervals). Position of the input box is ignored; packing is generated at the origin. |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Packed Offcuts | POc | Brep | List | Solids that fit. Items that did not fit are omitted. |
| Container | C | Brep | Item | Origin-aligned box of the same size (viewport-hidden). |

---

## Fabricate

Move pieces to a machine plane, read/write files, and round-trip Offcut data.

### Orient on Plane (`OrientOc`)

**What it does:** Transforms each aligned Offcut (geometry and all planes) from a chosen local plane onto a target plane — typically World XY for CAM. Also rebuilds a **stock** box (uncut rectangular prism) in the same pose so you can compare blank vs machined shape.

**Inputs**

| Name | Nick | Type | Access | Default | Description |
| --- | --- | --- | --- | --- | --- |
| Aligned Offcuts | AOc | Offcut | List | — | Pieces with planes set. |
| Target Plane | TP | Plane | Item | World XY | Where to send the chosen local plane. |
| Plane Index | PI | Integer | Item | `0` | Which Offcut plane to match: `0` Base, `1` First, `2` Second, `3` Average. |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Oriented Offcuts | OOc | Offcut | List | Fabricated solids and planes in the new pose. |
| Offcut Stock | OcS | Brep | List | Rectangular blanks (viewport-hidden). |

---

### Get Coordinates (`Coords`)

**What it does:** Reads a CSV of **points** (not Offcuts). Each row: `x`, `y`, `z` after splitting on the delimiter.

**Inputs**

| Name | Nick | Type | Access | Default | Description |
| --- | --- | --- | --- | --- | --- |
| File Path | file | Text | Item | — | Path to CSV. |
| Delimiter | D | Text | Item | `;` | Split character (first character only). |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Coordinates | C | Point | List | One point per row. |

---

### Get CSV Files (`CSV`)

**What it does:** Lists every `*.csv` in a folder (top level only, no subfolders).

**Inputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Folder Path | F | Text | Item | Directory path. |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| File Paths | FP | Text | Tree | One branch per file, containing the full path. |

---

### Offcut to JSON (`To JSON`)

**What it does:** Serializes a list of Offcuts to a JSON file (Newtonsoft.Json). Overwrites the path if the file exists.

**Inputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Offcut Data | OcD | Offcut | List | Data to save (including geometry/planes if present). |
| File Path | file | Text | Item | Destination path including filename. |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| File Path | file | Text | Item | Same path after a successful write. |

---

### JSON to Offcut (`To Offcut`)

**What it does:** Inverse of Offcut to JSON. Reads a JSON array of Offcuts.

**Inputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| File Path | file | Text | Item | Path to the JSON file. |

**Outputs**

| Name | Nick | Type | Access | Description |
| --- | --- | --- | --- | --- |
| Offcut Data | OcD | Offcut | List | Deserialized Offcuts. |

---

## Suggested wiring

```text
CSV / Excel / Construct Offcut
        ↓ OcD
Curve Alignment  or  Optimized Alignment  ←  design Curve
        ↓ AOc                    ↓ UOc  (next curve or packing)
   Unification (optional)
        ↓
Tenon Joints  or  Spline Joints
        ↓
Find Intersections (centroid curves) → Intersection Joints
        ↓
Orient on Plane → CAM / bake
        ↓
Offcut to JSON  (optional archive)
```

Example Grasshopper files live under `Documentation/Examples` and `Documentation/Reproduce` as described in the main README.
