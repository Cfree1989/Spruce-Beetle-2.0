# Packing joints — component spec

Thesis working spec for the **packed-column joint path**. It records intended function vs what is in the plugin today, plus known issues, **before** more Grasshopper edits.

This is **not** the user-facing [Component-Reference.md](Component-Reference.md). That file is already stale (packing still described as Brep-only; no Packed Stacks / Contacts). Sync it after we ship the Contact Tenon edits.

No C# changes in this pass. Rename, extra knobs, and placement modes below are **proposed**.

Related: [joint placement.png](../joint%20placement.png) at the repo root (Seam placement target), [Column-Fill-2x2x8.md](Column-Fill-2x2x8.md) (pack a 24×24×96 box; still says packing does not make joints).

---

## Why this exists

Four packing-joint pieces landed without a written contract: **Packed Stacks**, **Packed Contacts**, **Select Contacts**, and **Contact Joints**. Alignment already has **Tenon Joints** and **Spline Joints**. This spec says what each packing component is for, what it actually does, and what to change next.

---

## Two parallel paths (keep both)

Shared source: **Bin Packing EB-AFIT** (`PackBinC#`) output `Oc` — packed Offcuts with rotated size, closed Brep, and Z-end planes (`FirstPlane` / `SecondPlane` at the bottom and top of each piece).

```text
Bin Packing EB-AFIT  →  Oc
        ├─ Packed Stacks  →  graft  →  Tenon Joints
        │     Z-columns only, joints centered on each Z-bed
        │
        └─ Packed Contacts → Select Contacts → Contact Tenon
              Z beds + XY stitches; which contacts; then cut
```

| Path | What it joins | Where the joint sits | Cutter |
| --- | --- | --- | --- |
| Packed Stacks | Consecutive pieces in a Z-stack | Face center (Alignment Tenon) | Alignment **Tenon Joints** (`JX`/`JY`/`JZ`/`R`) |
| Contacts | Selected face pairs, including side faces | Placement mode (default Seam) | **Contact Tenon** (today Contact Joints) |

**Spline Joints** stays on the curve-alignment tab. A later **Contact Spline** is a name only — not specified here.

Do **not** wire Select Contacts into Alignment Tenon. Tenon takes an ordered Offcut list and cuts `FirstPlane`/`SecondPlane` only. It does not read `PackedContact`.

---

## Shared source: Bin Packing `Oc`

**Intended job:** Give packing a jointable Offcut list, not only preview solids.

**As-built:** Third output `Oc` (GUID `99C99B34-2B2F-418D-AB51-F3A139064C10` unchanged). Planes are World Z-up at the piece bottom/top centers. Container pose is ignored (origin-aligned box). Pieces that do not fit are omitted (no unused list).

**Issues:** Component-Reference still says geometry only. Column-Fill tutorial does not mention `Oc` or joints.

---

## Packed Stacks (`PackStacks`)

**Intended job:** Group packed pieces that touch face-to-face along **Z**, ordered bottom to top, so Alignment Tenon can run on each stack. Keep this as a parallel option for centered Z tenons. Isolated pieces (no Z neighbor) do not get a stack branch.

**Inputs / outputs (intended = as-built)**

| | Nick | Type | Notes |
| --- | --- | --- | --- |
| In | `Oc` | Offcut list | Packed Offcuts |
| In | `T` | Number | Gap treated as Z contact (default `0.01`) |
| Out | `S` | Offcut tree | One branch per stack of 2+ pieces; graft into Tenon |
| Out | `I` | Offcut list | Isolated pieces |
| Out | `P` | Planes | Z interface planes (overlap centers) |

**As-built:** [Packing/PackedStacks_GH.cs](../Packing/PackedStacks_GH.cs). GUID `8F3A6C21-4B9E-4D17-9A55-E2C8B1F04673`. Uses `PackedNeighbors.ZStacks`. Icon is Find Intersections (cosmetic debt; not a rename).

**Issues / adjustments**

- Keep. Do not retire when Contact Tenon exists.
- Tenon on packed planes is Z-up, not curve-aligned; confirm in GH that end cuts land on the beds.
- XY side faces are **out of scope** for this path (that is Packed Contacts).

---

## Packed Contacts (`PackContacts`)

**Intended job:** List every **face-to-face** contact between packed Offcuts (Z beds and XY stitches). Does not cut wood. Does not choose which contacts get joints. Contact plane is the **center** of the overlap rectangle; placement offset happens later on Contact Tenon.

**Inputs / outputs (intended = as-built)**

| | Nick | Type | Notes |
| --- | --- | --- | --- |
| In | `Oc` | Offcut list | Same list Contact Tenon will cut |
| In | `T` | Number | Max gap for a contact (default `0.01`) |
| Out | `C` | `PackedContact` list | Pair indices, axis, overlap, center plane |
| Out | `P` | Planes | Same planes as `C` (preview) |
| Out | `R` | Curves | Overlap rectangles |
| Out | `A` | Text | `Z`, `X`, or `Y` |

A `PackedContact` is two packed indices plus `ContactAxis`, overlap box, area, and that center plane ([Packing/PackedNeighbors.cs](../Packing/PackedNeighbors.cs)).

**As-built:** [Packing/PackedContacts_GH.cs](../Packing/PackedContacts_GH.cs). Nickname `PackContacts`. GUID `B4C8E2A1-7F3D-4B19-9E6C-2A5D8F1B0473`. Pairwise: Z first, else X, else Y (one axis per pair). Column test (older `stock_column_in.csv` pack, ~33 pieces): 88 contacts (28 Z, 60 XY).

**Issues / adjustments**

- Function is correct enough. Keep as the contact enumerator.
- One contact per pair/axis: a pair with both a Z bed and an XY stitch cannot report both (Z wins). Accept unless we see missed stitches.

---

## Select Contacts (`PickJoints`)

**Intended job:** Choose **which** contacts become joints. Filter only; no geometry.

**Inputs / outputs**

| | Nick | Type | Notes |
| --- | --- | --- | --- |
| In | `C` | Contacts | From Packed Contacts |
| In | `N` | Integer | Piece count for Connected (optional; inferred from max index) |
| In | `M` | Text | Mode (value list auto-added) |
| In | `T` | Number | Seam-corner gap (default `0.01`) |
| Out | `C` | Contacts | Kept subset |
| Out | `P` | Planes | Planes of kept contacts |

**Modes (keep these names)**

| Mode | Intended | As-built |
| --- | --- | --- |
| All | Every contact | Same |
| Z | Z beds only | Same |
| XY | X or Y stitches only | Same |
| Seams | Contacts that meet a **T-junction / third piece**, matching the red ticks on [joint placement.png](../joint%20placement.png) | `SharesSeam`: keep a contact if its overlap box is near any other contact of a **different** axis. Too loose — column test kept **88 of 88**. |
| Connected | Small set that still ties the pack together (prefer seams, then Z, then XY by area) | `SelectConnected` union-find; stops when joinable pieces are one component |

**As-built:** [Packing/SelectContacts_GH.cs](../Packing/SelectContacts_GH.cs). GUID `1D9A6E40-C3B2-4F58-A817-6E0C4D92F1AB`. Icon is Unification (cosmetic).

**Issues / adjustments**

- **Seams filter is a known bug**, separate from Contact Tenon **Seam placement**. Tighten later so Seams matches the red-tick sketch. Do not implement in this doc pass.
- Connected vs Z-only still needs a real column test after Seams is trustworthy.
- Function of All / Z / XY is correct enough.

---

## Contact Tenon (today: Contact Joints)

**Intended job:** Cut a **matching tenon pocket** on both pieces at each selected contact. Packing cutter, not a feeder into Alignment Tenon. Same idea as Alignment Tenon: boolean-difference the same solid from both members; output `J` is the tenon body (loose tenon / key), not a male stub left on one stick.

A later **Contact Spline** would be a different cutter (dovetail / through key). Do not overload this component.

**Rename (when we edit code)**

| | Today | Spec |
| --- | --- | --- |
| Display name | Contact Joints | **Contact Tenon** |
| Nickname | `PackJoints` | **PackTenon** |
| GUID | `7C2F5B18-E9A4-4D06-B3C1-8F47A0E256D9` | **unchanged** |
| Icon | Spline Joints bitmap | Tenon bitmap or a new icon |

**As-built:** [Packing/ContactJoints_GH.cs](../Packing/ContactJoints_GH.cs)

| | Nick | Rule |
| --- | --- | --- |
| In `Oc` | Packed Offcuts | Same list / same order as Packed Contacts |
| In `C` | Selected contacts | |
| In `D` | Tool diameter | Default `0.25`. Drives width, depth cap, fillet, **and** inset |
| In `W` | Width factor | Pocket width = `W × D` (default `1`) |
| Out `Oc` | Cut Offcuts | Failed boolean keeps last successful solid |
| Out `J` | Pocket solids | One cutter per successful contact |
| Out `Sk` | Skipped planes | Overlap too small, depth 0, or boolean fail |

Geometry rules today:

- Square pocket `width × width`, fillet `D/2`
- Depth = `min(D, thinner member / 3)` along the contact axis
- Placement = always `OffsetTowardSeam`: inset **`D`** toward a detected third-piece edge; if none fits, skip. Prefer seam edges, then shorter half-span.
- Column test: 23 pockets cut, 65 skipped (`D = 0.25"` and/or boolean fail)

**Issues / adjustments**

- Depth, inset, and placement are not independently adjustable. That is the next code pass (below), not this doc.
- Many skips: overlap smaller than `D` + pocket, or `CreateBooleanDifference` fails.
- Output description still says “keys / splines”; after rename, call `J` the tenon solids.

### Proposed inputs (not built)

Defaults should match today’s look so old canvases do not jump.

| Name | Nick | Default | Role |
| --- | --- | --- | --- |
| Tool Diameter | `D` | `0.25` | Mill constraint, skip test, default fillet `D/2` |
| Width Factor | `W` | `1` | Pocket width = `W × D` |
| Depth | `Dep` | `D` | Requested cut depth; still clamp to `thinner / 3` |
| Inset | `I` | `D` | Distance from the chosen overlap edge / T-junction |
| Place | `Place` | `Seam` | **Center** / **Edge** / **Seam** |

**Placement modes (intended)**

- **Center** — origin at the overlap rectangle center (Alignment Tenon analogue). `I` unused.
- **Edge** — inset `I` toward the **shortest** overlap edge (no third-piece test).
- **Seam** (default) — inset `I` toward a **T-junction / third-piece corner**, as marked in [joint placement.png](../joint%20placement.png). If no third piece, fall back to Edge.

Later (not required for the first Contact Tenon edit): pocket **length** along the seam (non-square slot), explicit fillet `R` instead of `D/2`.

---

## What Alignment Tenon / Spline already do

Do not duplicate these on the packing tab.

**Tenon Joints** (`Tenon`): ordered `AOc` chain; cut `FirstPlane`/`SecondPlane`; size `JX`/`JY`/`JZ`; `R` is fillet only; types tenon / cross / custom.

**Spline Joints** (`Spline`): same chain; dovetail slots through Y; not for packed XY faces.

Packed Stacks is the only packing → Alignment Tenon hookup.

---

## Out of scope (this doc pass)

- Any C# / Grasshopper edit, GUID change, or in-plugin rename
- Contact Spline design
- Rewriting Column-Fill-2x2x8.md or Component-Reference.md
- Tightening the Seams filter (record the bug only)

---

## Open questions (resolve when we implement Contact Tenon)

1. **`Dep` as a length vs a factor** — spec uses a length defaulting to `D`, clamped to thinner/3. A factor (`0.33` of thinner) would scale with stock; pick one in the first code PR.
2. **Seams filter vs Seam placement** — different: Select Contacts *which* faces; Contact Tenon *where on the face*. Tighten the filter in a separate change.
3. **Skip vs fail** — keep skipped planes, or also output failed cutters / a text report?
4. **Connected mode** — keep after Seams works, or drop if Z + Seams is enough for the column?
5. **Male tenon vs matching pocket** — Alignment Tenon already cuts both sides and outputs `J` as the body. Contact Tenon stays matching unless we explicitly want a stub left on one piece.

---

## Next code pass (after this spec is accepted)

1. Rename Contact Joints → Contact Tenon (`PackTenon`), GUID unchanged, fix icon/description.
2. Add `Dep`, `I`, `Place` (Center / Edge / Seam).
3. Then, separately: tighten Select Contacts **Seams** to T-junctions.
4. Then: Component-Reference packing section + Column-Fill joint wiring.
