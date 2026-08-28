# Fill a 2' × 2' × 8' column with Offcuts

Use Spruce Beetle **Bin Packing EB-AFIT** to pack leftover rectangular parts into a building column. This is **not** curve alignment. You make a box; the plugin fills it with Offcut stock.

This guide assumes Rhino **model units = Inches**.

```text
CSV of parts  →  CSV to Offcut  →  Offcut list
24 × 24 × 96 Box  ──────────────→  Bin Packing EB-AFIT  →  packed solids
```

---

## 0. Rhino units

Your file is in **inches**. Keep it that way.

- Column size in the model: **24 × 24 × 96** (that is 2' × 2' × 8').
- CSV numbers are inches too.
- Do not type `2`, `2`, `8` into Grasshopper — that would be a 2" × 2" × 8" stick.

The plugin does not convert units. The CSV, the Box, and Rhino must all be inches.

---

## 1. Stock file

Use this file (inches):

`Documentation/TestData/stock_column_in.csv`

Format (required by **CSV to Offcut**):

```text
index;x;y;z
```

No header. Semicolon delimiter. Example:

```text
1;6;6;24
```

That is a **6" × 6" × 24"** block. The file mixes 3", 6", 12", and 24" sections and lengths of 12", 24", 48", and 96".

Full path:

`C:\Repos\Spruce-Beetle-2.0\Documentation\TestData\stock_column_in.csv`

The other files in `Documentation/TestData/` are also in **inches**. For this column, still use `stock_column_in.csv`.

---

## 2. Grasshopper — load parts

1. Open Grasshopper.
2. Tab **Spruce Beetle** → **Create** → **CSV to Offcut**  
   Nickname: **CSV 2 Oc**.
3. Drop a **Panel**. Paste the full path to `stock_column_in.csv`.
4. Connect Panel → **File Path (`file`)**.
5. **Delimiter (`D`)** defaults to `;`. Leave it, or Panel with `;`.

You should get a list of Offcuts. If the component goes red: path is wrong, or delimiter is not `;`.

**Without a CSV:** **Construct Offcut** (`ConOffcut`) with three equal-length number lists for x, y, z **in inches**.

---

## 3. Grasshopper — the column volume

Use the 24×24×96 box already in Rhino, or build it in Grasshopper.

### Option A — reference the Rhino box

1. **Params → Geometry → Box** (or Brep, then **Bounding Box**).
2. Right-click → **Set one Box** and pick the column in the viewport.

If you drew it as 2×2×8 **while units were feet**, it may still be the right physical size now that units are inches (Rhino converts geometry when you change units). Check the size: it should measure **24 × 24 × 96**. If it measures 2×2×8, it is 2 inches on a side — delete it and draw 24×24×96.

The packer uses the box **size**, not its location. Output is always built at the **World origin**. Step 5 moves it back onto your column.

### Option B — build the box in Grasshopper

1. **Params → Geometry → Plane** → **XY Plane** (World XY).
2. **Surface → Primitive → Box**.
3. Three **Number Sliders** (or Panels):

   | Box domain | Value in inches |
   | --- | --- |
   | X | `0` to `24` |
   | Y | `0` to `24` |
   | Z | `0` to `96` |

That is a 2' × 2' × 8' prism standing on World XY.

It must be a Grasshopper **Box**. A Brep is not enough: **Bounding Box** first, then connect that Box.

---

## 4. Pack the column

1. **Spruce Beetle** → **Packing** → **Bin Packing EB-AFIT**  
   Nickname: **PackBinC#**.
2. Connect:

   | From | To |
   | --- | --- |
   | CSV to Offcut → **Offcut Data** | **Offcut Data (`OcD`)** |
   | Box | **Box (`B`)** |

3. Outputs:

   | Output | Nickname | What you get |
   | --- | --- | --- |
   | Packed Offcuts | `POc` | Solids inside the 24×24×96 volume (at the origin) |
   | Container | `C` | The empty 24×24×96 box at the origin (preview often hidden; right-click output → Preview) |

4. Zoom the Rhino viewport to **0,0,0**. You should see a 2'×2'×8' volume filled with smaller boxes.
5. **Bake** Packed Offcuts when you want them in the document.

Pieces may be **rotated**. Anything that does not fit is **left out** (no leftover list). Compare list lengths: Offcut count vs Packed Offcuts count.

If the fill looks tiny: the Box is still 2×2×8 (inches) instead of 24×24×96.

---

## 5. Move the pack onto your column (if the Rhino box is not at the origin)

Packing is generated with the container sitting on World XY, from `(0,0,0)` in +X, +Y, +Z.

1. **Deconstruct Box** on your design Box → **Plane**.
2. **Transform → Affine → Orient**:
   - **Geometry:** Packed Offcuts
   - **Source:** World XY (origin)
   - **Target:** the Box plane from step 1

Bake the oriented geometry. That is your column fill in place.

---

## 6. Graph to copy

```text
[Panel: path to stock_column_in.csv]
        │
        ▼
CSV to Offcut  (D = ;)  ── OcD ──► Bin Packing EB-AFIT ──► Packed Offcuts ──► Orient (optional) ──► Bake
                                      ▲
XY Plane → Box (0–24, 0–24, 0–96) ── B┘
                                      └──► Container (preview the 24×24×96)
```

---

## 7. What this will not do

- It will **not** follow a curve or make timber joints. That is **Curve Alignment** / **Tenon Joints**.
- It will **not** fill an arbitrary Brep (tapered column, fluted section). Only a **rectangular** Box.
- It will **not** guarantee a 100% fill. EB-AFIT is a fast packer, not a perfect one. Add more stock or smaller pieces if you see voids.
- It will **not** keep Offcut IDs on the output — you get **Breps** only.

---

## 8. Tweaking the fill

| Goal | What to change |
| --- | --- |
| More solid column | Add more rows to the CSV, or smaller x/y (still ≤ 24) |
| Fewer leftover gaps | Mix of 6" and 12" sections (already in `stock_column_in.csv`) |
| Fewer parts / bigger blocks | Delete small rows; keep 12×12×24 and 12×12×48 |
| See unused stock | List Length on Offcuts vs Packed Offcuts |
| Different column size | Change the Box domains; keep CSV units in inches |

Edit `stock_column_in.csv` with any editor. Keep `index;x;y;z` and `;`. Every x and y should be **≤ 24**, every z **≤ 96**, or that piece cannot go in (unless rotation swaps axes — a 6×6×96 stick can stand as a full-height corner).
