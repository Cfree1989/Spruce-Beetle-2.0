# Stock bank and photo measure

Working design for the next build. **Measure** and **Stock Bank** are not in the plugin yet. What exists today is [CSV to Offcut](Component-Reference.md) and [Remaining Offcuts](Component-Reference.md). How a column is packed once the stock list exists: [Column-Fill-2x2x8.md](Column-Fill-2x2x8.md).

Shop notes from 16 November 2026: the first column took most of a day, including figuring out import, the setup, and the export script. Cutting was about two and a half hours and the build about three, done in segments. The second column took about 30 minutes to import and about two hours to cut and build. Cutting and stacking are fast enough. The slow work is measuring each board and getting new boards into the old unused groups.

---

## What the plugin does today

Each physical board is an **Offcut**. The number written on the board is the Offcut **Index**. A stock file is one row per board, `index;x;y;z`, in the Rhino document’s units. Test stock is in inches. **CSV to Offcut** (`CSV 2 Oc`) reads that file.

**Remaining Offcuts** (`Remain`) makes the leftovers. Merge every stock CSV into **Pile**. Merge each packed column `Oc` into **Used**. The output is Pile with those scrap numbers removed. One pin takes one wire, so a Grasshopper **Merge** sits in front of each pin whenever a column has more than one source.

That is the list-making the stock bank is meant to replace.

---

## The stand

The camera looks straight down and does not move during a session. A measuring square of known side length stays in the frame. Boards are laid flat in that view, on the square or beside it, with the square still visible.

The square is the scale. Its real side, in inches, is typed once. A top-down photo can see length and width. It cannot see thickness. Thickness is the pile setting for the boards being photographed.

The first check of a session is one board already measured with a tape. When the readout matches the tape, the rest of the pile uses the same setup.

---

## One bank

**Stock Bank** is one component. It remembers every scrap in the Grasshopper file: scrap number, thickness, width, length, and whether a column has claimed it. It can keep writing that same record out to one CSV.

Two things change the bank.

- **Measure** accepts a board. That board is added.
- A packed column’s `Oc` is wired into **Claim**. Those scrap numbers are marked used.

**Free** is every scrap in the bank that no column has claimed. The next column reads **Free**. There is no leftovers list and no merge.

A group is a thickness. The 1.5-inch pile is every free scrap whose thickness is 1.5. A new 1.5-inch board joins that pile because Accept wrote it into the bank.

---

## Session

1. Open the file. The bank already holds the scraps from earlier columns, used and free.
2. Set the measuring square’s real side on **Measure**.
3. Lay one taped board in the frame. Continue once the inches match.
4. Sort the floor into piles that share a thickness.
5. Set **Measure** to the first pile’s thickness. The window shows the next scrap number, one higher than the highest number already in the bank.

## Each board

1. Read the number on the window.
2. Write that number on the board.
3. Lay the board in the frame. The square stays visible.
4. The window draws a rectangle and shows length and width.
5. If the outline sits on a split or a dark edge, drag a corner onto the board you mean.
6. Accept.

Accept stores that Offcut under the number just written. The window advances to the next number. **Free** updates, and the new board is available to pack.

If the rectangle is wrong, fix the corners and accept that same board. The counter moves only after Accept, so the number already written on the wood is still the number being stored.

When the pile is finished, change the thickness and keep going.

## Pack the next column

Wire **Free** into the packer the same way a stock list is wired today. [Column-Fill-2x2x8.md](Column-Fill-2x2x8.md) is that path: stock into **Bin Packing EB-AFIT**, then **Used Offcuts** and **Label Offcut Numbers**.

Wire the packed `Oc` into **Claim**. Those numbers leave **Free**. They keep the numbers written on them. The pick list says “122” and the board says “122.”

**Label Offcut Numbers** cuts that same scrap number into a face at fabrication. The ink at the stand and the cut label are one integer.

The following column reads the same **Free** output. It sees boards photographed since the last pack, and it does not see boards already claimed.

---

## Canvas

```text
Stock Bank  →  Free  →  Bin Packing EB-AFIT  →  Oc  →  Claim
                 ↑                                    │
Measure (accept) ┘                                    └── back into the bank
```

**Measure** inputs for a session: the square’s side, and the thickness of the pile in front of the camera. The scrap number comes from the bank.

**Stock Bank** outputs **Free**. **Claim** takes packed `Oc`. Matching is by scrap number, which is what **Remaining Offcuts** already does between Pile and Used.
