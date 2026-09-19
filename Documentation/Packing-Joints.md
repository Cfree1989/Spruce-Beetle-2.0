# Packing joints — component spec

Thesis working spec for the **packed-column joint path**. User-facing tables live in [Component-Reference.md](Component-Reference.md). How to pack a 24×24×96 box: [Column-Fill-2x2x8.md](Column-Fill-2x2x8.md).

Placement is **decided**: tenons sit at the **center of the shared overlap rectangle**, with the tenon X axis along the longer in-plane side (2026-09-19 center; 2026-09-19 oriented frame). T-junction / seam placement was removed as too complicated.

Related: [joint-placement.png](joint-placement.png) (earlier Seam placement sketch, no longer the target).

---

## Why this exists

The packing tab enumerates face contacts and cuts matching tenons. Alignment already has **Tenon Joints** and **Spline Joints** for curve chains. This spec says what each packing joint component is for.

**Packed Stacks** (`PackStacks`, GUID `8F3A6C21-4B9E-4D17-9A55-E2C8B1F04673`) was deleted 2026-09-19. It existed only to graft Z-groups into Alignment Tenon. Union-find groups are not linear chains, and Tenon cuts on each piece's own end-plane center, so mating pockets did not coincide and joints appeared where there was no contact. Contact Tenon absorbs Tenon's knobs on a contact graph. Existing canvases that still contain Packed Stacks will show a missing-component placeholder.

---

## Joint path

Shared source: **Bin Packing EB-AFIT** (`PackBin`) output `Oc` — packed Offcuts with rotated size, closed Brep, and Z-end planes (`FirstPlane` / `SecondPlane` at the bottom and top of each piece).

```text
Bin Packing EB-AFIT  →  Oc
        └─ Packed Contacts → Select Contacts → Contact Tenon
              Z beds + XY stitches; which contacts; then cut
```

| Path | What it joins | Where the joint sits | Cutter |
| --- | --- | --- | --- |
| Contacts | Selected face pairs (Z beds and XY stitches) | Center of the shared overlap rectangle; X along the long side | **Contact Tenon** (`JX`/`JY`/`Dep`/`R`/`JT`/`TC`) |

**Spline Joints** stays on the curve-alignment tab. A later **Contact Spline** is a name only — not specified here.

Do **not** wire Select Contacts into Alignment Tenon. Tenon takes an ordered Offcut list and cuts `FirstPlane`/`SecondPlane` only. It does not read `PackedContact`.

---

## Shared source: Bin Packing `Oc`

**Intended job:** Give packing a jointable Offcut list, not only preview solids.

**As-built:** Third output `Oc` (GUID `99C99B34-2B2F-418D-AB51-F3A139064C10` unchanged). Nickname **PackBin** (was `PackBinC#`; `ApplyDisplayNames` retitles old canvases). Planes are World Z-up at the piece bottom/top centers. Container pose is ignored (origin-aligned box). Pieces that do not fit are omitted (no unused Offcut list). Index (CSV / written stock number) is kept on each packed Offcut; **Used Offcuts** (`UsedOc`) lists used and leftover numbers.

---

## Packed Contacts (`PackContacts`)

**Intended job:** List every **face-to-face** contact between packed Offcuts (Z beds and XY stitches). Does not cut wood. Does not choose which contacts get joints. Contact plane is the **center** of the overlap rectangle; Contact Tenon orients that frame so X follows the longer in-plane side.

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

**As-built:** [Packing/PackedContacts_GH.cs](../Packing/PackedContacts_GH.cs). Nickname `PackContacts`. GUID `B4C8E2A1-7F3D-4B19-9E6C-2A5D8F1B0473`. Icon is Find Intersections. Pairwise: Z first, else X, else Y (one axis per pair). Column test (older `stock_column_in.csv` pack, ~33 pieces): 88 contacts (28 Z, 60 XY).

**Issues / adjustments**

- Function is correct enough. Keep as the contact enumerator.
- One contact per pair/axis: a pair with both a Z bed and an XY stitch cannot report both (Z wins). Accept unless we see missed stitches.

---

## Select Contacts (`PickContacts`)

**Intended job:** Choose **which** contacts become joints. Filter only; no geometry.

**Inputs / outputs**

| | Nick | Type | Notes |
| --- | --- | --- | --- |
| In | `C` | Contacts | From Packed Contacts |
| In | `M` | Text | Mode (value list auto-added; All / Z / XY) |
| Out | `C` | Contacts | Kept subset |
| Out | `P` | Planes | Planes of kept contacts |

**Modes**

| Mode | Job |
| --- | --- |
| All | Every contact |
| Z | Z beds only |
| XY | X or Y stitches only |

**Seams** and **Connected** were removed 2026-09-19 with T-junction logic. Leftover `Seams` / `Connected` strings (old value lists) fall through to All with a warning. The auto value list is rewritten to All / Z / XY on solve.

**As-built:** [Packing/SelectContacts_GH.cs](../Packing/SelectContacts_GH.cs). Nickname **PickContacts** (was `PickJoints`; `ApplyDisplayNames` retitles old canvases). GUID `1D9A6E40-C3B2-4F58-A817-6E0C4D92F1AB`. Icon is Unification (cosmetic debt; no better existing bitmap).

**Issues / adjustments**

- All / Z / XY are the filter. Function is correct enough.

---

## Contact Tenon (`ContactTenon`)

**Intended job:** Cut a **matching tenon pocket** on both pieces at each selected contact. Packing cutter, not a feeder into Alignment Tenon. Same idea as Alignment Tenon: boolean-difference the same solid from both members; output `J` is the tenon body (loose tenon / key), not a male stub left on one stick.

A later **Contact Spline** would be a different cutter (dovetail / through key). Do not overload this component.

**As-built:** [Packing/ContactTenon_GH.cs](../Packing/ContactTenon_GH.cs) (was Contact Joints / `PackJoints`). Display **Contact Tenon**, nick **ContactTenon**, GUID `7C2F5B18-E9A4-4D06-B3C1-8F47A0E256D9` unchanged. Icon is Tenon Joints. `ApplyDisplayNames` retitles old canvases. Pin layout changed (`W` gone, `D` re-purposed); re-wire `D`/`JX`/`JY`/`Dep`/`R`/`JT`/`TC`.

| | Nick | Rule |
| --- | --- | --- |
| In `Oc` | Packed Offcuts | Same list / same order as Packed Contacts |
| In `C` | Selected contacts | |
| In `D` | Tool diameter | Default `0.25`. Mill constraint only: floors `JX`, `JY`, and `R`. Does not set size |
| In `JX` | Long-side size | Default `1`. Along the overlap's longer in-plane direction. Raised to `D` if smaller |
| In `JY` | Short-side size | Default `1`. Across the overlap. Raised to `D` if smaller |
| In `Dep` | Total depth | Default `0.5`. Centered on the contact plane; clamped to `thinner member / 3` |
| In `R` | Fillet radius | Default `0.125`. Raised to `D / 2` if smaller; still capped below `min(JX, JY) / 2` |
| In `JT` | Joint type | Auto value list: tenon / cross tenon / custom tenon |
| In `TC` | Tenon count | Default `1`. Spread along the long side |
| In `CS` | Custom curve | Optional closed planar curve |
| Out `Oc` | Cut Offcuts | Failed boolean keeps last successful solid |
| Out `J` | Tenon solids | One solid per tenon (TC per successful contact) |
| Out `JV` | Joint volumes | Volume of each `J` solid |
| Out `Sk` | Skipped planes | Overlap too small, depth 0, missing custom curve, or boolean fail |

Geometry rules:

- Placement = `OrientOnOverlap`: origin at the **center of the shared overlap rectangle**; plane X along the longer in-plane side, Y along the shorter. Skip if `JX × TC` exceeds the long side or `JY` exceeds the short side. No inset, no third-piece test.
- Depth = `min(Dep, thinner member / 3)` along the contact axis (total, centered, so each member takes half).
- `D` is a **constraint, not a size** (unlike the old Contact Joints, where pocket width was `W × D`). A ¼″ bit is `D = 0.25`; it cannot cut a tenon narrower than `0.25` or an inside corner tighter than `0.125`. `JX` / `JY` / `R` are real model-unit sizes; the component warns when it raises one of them.
- Types mirror Alignment Tenon (rect / cross / custom) but live in this file so Reisach's `TenonJoints_GH.cs` stays untouched.

Column test history: OffsetTowardSeam (before 2026-09-19) cut 23 / skipped 65. Center placement and full-parity knobs are **not yet re-counted** on the column CSV.

---

## What Alignment Tenon / Spline already do

Do not duplicate these on the packing tab.

**Tenon Joints** (`Tenon`): ordered `AOc` chain; cut `FirstPlane`/`SecondPlane`; size `JX`/`JY`/`JZ`; `R` is fillet only; types tenon / cross / custom.

**Spline Joints** (`Spline`): same chain; dovetail slots through Y; not for packed XY faces.

There is no packing → Alignment Tenon hookup.

---

## Out of scope (this spec)

- Contact Spline design
- New packing-tab icons (Select Contacts still borrows Unification)

---

## Next

1. Close Rhino and reload the `.gha`.
2. Re-run the column test (`stock_column_in.csv`); record cut / skipped counts under Contact Tenon above.
3. Re-wire old Contact Joints boxes (`W` is gone; `D` is now a floor, not a size).
