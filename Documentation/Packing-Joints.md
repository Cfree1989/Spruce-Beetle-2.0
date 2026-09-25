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
        └─ Packed Contacts → Select Contacts ┬→ Contact Tenon   (captured blind key)
                                             ├→ Contact Spline  (edge-open key, after stack)
                                             └→ Outside Key     (face key on the column skin)
```

| Path | What it joins | Where the joint sits | Cutter |
| --- | --- | --- | --- |
| Tenon | Selected face pairs (typical: Z beds) | Center of the shared overlap rectangle; X along the long side; inset `D` on all sides | **Contact Tenon** (`JX`/`JY`/`Dep`/`R`/`JT`/`TC`) |
| Spline | Selected face pairs (typical: XY stitches) | Same overlap; channel opens one free edge so a key can be driven in after both pieces are seated | **Contact Spline** (`JY`/`Dep`/`R`/`TC`; key length is the slot) |
| Outside key | Contacts that reach a vertical hull face | Pocket on the packed AABB skin, centered on the seam, half in each piece | **Outside Key** (`JX`/`JY`/`Dep`/`R`/`JT`/`TC`) |

**Spline Joints** stays on the curve-alignment tab (dovetail on a chain). **Contact Spline** is the packing cutter: a rectangular edge-open slot, not a dovetail. **Outside Key** is a different cut: a pocket in the outer face, not a slot along the mating plane. Wire Contact Tenon `Oc` into Outside Key when Z tenons are cut first. The same contact sent to more than one cutter gets each cut.

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
| Out | `P` | Planes | Same planes as `C`. Preview only; component Bake skips planes |
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
| Out | `P` | Planes | Planes of kept contacts. Preview only; component Bake skips planes |

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

Do not overload this component with edge-open slots — that is **Contact Spline**.

**As-built:** [Packing/ContactTenon_GH.cs](../Packing/ContactTenon_GH.cs) (was Contact Joints / `PackJoints`). Display **Contact Tenon**, nick **ContactTenon**, GUID `7C2F5B18-E9A4-4D06-B3C1-8F47A0E256D9` unchanged. Icon is Tenon Joints. `ApplyDisplayNames` retitles old canvases. Pin layout changed (`W` gone, `D` re-purposed); re-wire `D`/`JX`/`JY`/`Dep`/`R`/`JT`/`TC`.

| | Nick | Rule |
| --- | --- | --- |
| In `Oc` | Packed Offcuts | Same list / same order as Packed Contacts |
| In `C` | Selected contacts | |
| In `D` | Tool diameter | Default `0.25`. Mill constraint only: floors `JX`, `JY`, and `R`. Does not set size |
| In `JX` | Long-side size | Default `1`. Along the overlap's longer in-plane direction. Raised to `D` if smaller |
| In `JY` | Short-side size | Default `1`. Across the overlap. Raised to `D` if smaller |
| In `Dep` | Total depth | Default `0.5`. Centered on the contact plane; clamped to `thinner member / 3` |
| In `R` | Fillet radius | Default `0.125`. Raised to `D / 2` if smaller (silent); still capped below `min(JX, JY) / 2` |
| In `JT` | Joint type | Auto value list: tenon / cross tenon / custom tenon |
| In `TC` | Tenon count | Default `1`. Spread along the long side |
| In `CS` | Custom curve | Optional closed planar curve |
| In `Cl` | Clearance | Default `0.005`. Auto slider `0.001`–`0.01`. Gap on each side; pocket is `JX`/`JY`/`Dep` plus `2 × Cl`. `J` is the key |
| Out `Oc` | Cut Offcuts | Failed boolean keeps last successful solid |
| Out `J` | Tenon solids | One solid per tenon (TC per successful contact) |
| Out `JV` | Joint volumes | Volume of each `J` solid |
| Out `Sk` | Skipped planes | Preview in the viewport. Not baked with the component |
| Out `SkC` | Skipped contacts | Wire into a second Contact Tenon `C` with a smaller `JX` / `JY`. Use the first component's `Oc` as that second `Oc` |

Geometry rules:

- Placement = `OrientOnOverlap`: origin at the **center of the shared overlap rectangle**; plane X along the longer in-plane side, Y along the shorter.
- Edge meat: skip unless the tenon fits inside the overlap **inset by `D` on all sides** (`JX × TC ≤ long − 2D` and `JY ≤ short − 2D`). Flush-to-edge pockets that chewed the rim are skipped instead of cut.
- Depth = `min(Dep, thinner member / 3)` along the contact axis (total, centered, so each member takes half).
- `D` is a **constraint, not a size** (unlike the old Contact Joints, where pocket width was `W × D`). A ¼″ bit is `D = 0.25`; it cannot cut a tenon narrower than `0.25` or an inside corner tighter than `0.125`. `JX` / `JY` / `R` are real model-unit sizes. Raising `JX` / `JY` to `D` still warns; raising `R` to `D / 2` is silent.
- Types mirror Alignment Tenon (rect / cross / custom) but live in this file so Reisach's `TenonJoints_GH.cs` stays untouched.

Column test history: OffsetTowardSeam (before 2026-09-19) cut 23 / skipped 65. Center placement and full-parity knobs are **not yet re-counted** on the column CSV.

---

## Contact Spline (`ContactSpline`)

**Intended job:** Cut an **edge-open rectangular slot** on both members of a packed contact so a loose key can be driven in **after** the scraps are already stacked. Solves the assembly deadlock of two perpendicular captured tenons on one stick. Does not replace Contact Tenon.

**As-built:** [Packing/ContactSpline_GH.cs](../Packing/ContactSpline_GH.cs). Display **Contact Spline**, nick **ContactSpline**, GUID `A8D31C47-6E2B-4F90-9C14-5B7A2E8D0146`. Icon is Alignment Spline Joints (cosmetic debt). New component.

| | Nick | Rule |
| --- | --- | --- |
| In `Oc` | Packed Offcuts | Same list / same order as Packed Contacts |
| In `C` | Selected contacts | |
| In `D` | Tool diameter | Default `0.25`. Floors `JY` and `R`. Does not set size |
| In `JX` | Ignored | Pin kept so later wires do not shift. Key length is the slot, not this number |
| In `JY` | Slot width | Default `1`. Across the run. Raised to `D` if smaller |
| In `Dep` | Total depth | Default `0.5`. Centered; clamped to `thinner member / 3` |
| In `R` | Fillet radius | Default `0.125`. Raised to `D / 2` if smaller (silent) |
| In `TC` | Channel count | Default `1`. Parallel channels across the overlap |
| In `Cl` | Clearance | Default `0.005`. Auto slider `0.001`–`0.01`. Widens and deepens the slot, and lengthens the closed stop, by `Cl` per side. `J` stays the key |
| Out `Oc` | Cut Offcuts | Failed boolean keeps last successful solid |
| Out `J` | Key solids | One key per channel, from the open edge to the closed stop |
| Out `JV` | Joint volumes | Volume of each `J` solid |
| Out `Sk` | Skipped planes | Preview only; not baked with the component |
| Out `SkC` | Skipped contacts | Wire into a second Contact Spline `C` with a smaller `JY`. Second pass `Oc` is the first pass `Oc` |
| Out `Dir` | Drive-in lines | Mouth toward the closed stop |

Geometry rules:

- Placement frame from `TrySplineMouth` in [Packing/PackedNeighbors.cs](../Packing/PackedNeighbors.cs): overlap center; X from mouth toward stop; depth along the contact axis.
- Mouth search: a probe just outside the overlap edge, as long as the key, must miss every packed box except the two members. Order: world **+Z** when the contact is vertical (`X` or `Y`), then either long-side end, then either short-side end (run and width swap). First free mouth wins.
- Key length is the slot: open edge to the closed stop (`run − D`). `JX` does not shorten it. Closed end and both long edges keep `D` of meat (`JY × TC ≤ across − 2D`). Mouth is flush and the cutter overruns the edge by `0.01` so the boolean opens; the key stops at the edge. Mill fillet `R` is only on the **closed-stop** corners; the mouth meets the member edge at 90 degrees.
- Skip if no free mouth, the slot does not fit, depth is 0, or the boolean fails. A bed whose side mouth is blocked by a neighbor in the same course stays on Contact Tenon or unjointed.

---

## Outside Key (`OutsideKey`)

**Intended job:** Cut a **face key** into the packed column’s vertical skin, centered on a seam where both members share that outer face. The same pocket is boolean-differenced from both Offcuts; `J` is the loose key inserted after the stack is up. This is not Contact Spline (a channel along the mating face). Interior seams and stepped faces (one piece set back from the hull) are skipped.

**As-built:** [Packing/OutsideKey_GH.cs](../Packing/OutsideKey_GH.cs). Display **Outside Key**, nick **OutsideKey**, GUID `9B4E7D12-5A83-4C6F-B291-0E8F3A47D5C1`. Icon is Alignment Spline Joints (cosmetic debt). New component. Seat search: `TryOutsideSeats` in [Packing/PackedNeighbors.cs](../Packing/PackedNeighbors.cs).

| | Nick | Rule |
| --- | --- | --- |
| In `Oc` | Packed Offcuts | Same list / same order as Packed Contacts |
| In `C` | Selected contacts | Feed All, or both Z and XY. Horizontal skin seams come from Z beds; vertical skin seams from X/Y stitches |
| In `D` | Tool diameter | Default `0.25`. Floors `JX`, `JY`, and `R` |
| In `JX` | Along the seam | Default `1`. Raised to `D` if smaller |
| In `JY` | Across the seam | Default `1`. Split into both pieces. Raised to `D` if smaller |
| In `Dep` | Depth into the face | Default `0.5`. Clamped to `thinner member / 3` along the inward axis |
| In `R` | Fillet radius | Default `0.125`. Rectangular keys only. Raised to `D / 2` if smaller (silent) |
| In `JT` | Joint type | Auto value list: `rectangular` / `custom key` |
| In `TC` | Key count | Default `1`. Spread along the exposed seam |
| In `CS` | Custom curve | Optional closed planar curve; scaled into `JX` × `JY` |
| In `Cl` | Clearance | Default `0.005`. Auto slider `0.001`–`0.01`. Gap on each side in the face and extra depth inward. `J` stays the key |
| Out `Oc` | Cut Offcuts | Failed boolean keeps last successful solid |
| Out `J` | Key solids | Flush with the outer face; one per key |
| Out `JV` | Joint volumes | Volume of each `J` solid |
| Out `Sk` | Skipped planes | Preview only; not baked with the component |
| Out `SkC` | Skipped contacts | Wire into a second Outside Key `C` with a smaller `JX` / `JY`. Second pass `Oc` is the first pass `Oc` |
| Out `Dir` | Drive-in lines | From just outside the face inward |

Geometry rules:

- Hull is the AABB of the packed Offcut boxes (not a separate Box input). A seat exists only on ±X or ±Y of that hull, and only when both members and the overlap sit on that face (`0.01`). Top and bottom faces are not used.
- Frame: origin at the seam center on the face; X along the seam; Y across the seam; Z into the wood. One contact can seat on two sides (a corner).
- Fit: `JX × TC ≤ seam − 2D`, and `JY / 2` must fit in each member across the seam. Depth = `min(Dep, thinner inward thickness / 3)`.
- Rectangular profile is filleted with `R`. A custom curve is extruded as drawn (a bowtie stays a bowtie). The cutter overruns the face by `0.01`; `J` stops flush.
- Skip the contact when there is no skin seat, nothing fits, custom curve is missing, or every seat fails the boolean.

---

## What Alignment Tenon / Spline already do

Do not duplicate these on the packing tab.

**Tenon Joints** (`Tenon`): ordered `AOc` chain; cut `FirstPlane`/`SecondPlane`; mill constraint `D`; size `JX`/`JY`/`JZ`; optional `R` (unwired = `D / 2`); types tenon / cross / custom.

**Spline Joints** (`Spline`): same chain; dovetail slots through Y; not for packed XY faces.

There is no packing → Alignment Tenon hookup.

---

## Out of scope (this spec)

- New packing-tab icons (Select Contacts still borrows Unification; Contact Spline and Outside Key borrow Alignment Spline Joints)

---

## Next

1. Close Rhino and reload the `.gha`.
2. Re-run the column test (`stock_column_in.csv`); record cut / skipped counts under Contact Tenon, Contact Spline, and Outside Key (skin vs stepped vs interior).
3. Re-wire old Contact Joints boxes (`W` is gone; `D` is now a floor, not a size).
