---
name: ground-storable-piles
description: 'Use when adding or changing Vintage Story item ground piles, GroundStorable behavior, stacking layouts, pile models, or stack capacity and transfer settings in Signals.'
---

# Create a Ground-Storable Item Pile

Use the game-provided `GroundStorable` item behavior to let an item be placed and collected in a ground pile. The Signals [insulator itemtype](../../../assets/signals/itemtypes/insulator.json), [pile shape](../../../assets/signals/shapes/block/insulatorpile.json), and [held item shape](../../../assets/signals/shapes/item/insulator.json) provide a concrete `Stacking` example. The [wire itemtype](../../../assets/signals/itemtypes/el_wire.json) demonstrates `Quadrants`, a different ground layout rather than a multi-item stack.

## Procedure

1. **Inspect the item definition.** Find its itemtype JSON under `assets/signals/itemtypes/`. Check existing `behaviors`, variant groups and variant-specific properties, textures, maximum stack size, and its regular `shape`. Preserve unrelated behavior and item settings.

2. **Add `GroundStorable` with `Stacking`.** Put it in the item's top-level `behaviors` array if the same pile behavior applies to every variant. If only particular variants should be ground-storable or need different properties, follow the existing `behaviorsByType` pattern used by the game's stone item; inspect the installed game assets before copying that structure. Do not use `Quadrants` for a multi-item pile: it is a different ground layout.

   A minimal starting point is:

   ```json
   {
     "behaviors": [
       {
         "name": "GroundStorable",
         "properties": {
           "layout": "Stacking",
           "stackingModel": "signals:block/examplepile",
           "stackingCapacity": 16,
           "transferQuantity": 1,
           "bulkTransferQuantity": 4,
           "collisionBox": { "x1": 0, "y1": 0, "z1": 0, "x2": 1, "y2": 0.25, "z2": 1 },
           "cbScaleYByLayer": 0.0625
         }
       }
     ]
   }
   ```

  This is an illustrative starting point, not a list of universally required properties or recommended values. Tune capacity and transfer amounts to the intended gameplay. Set a collision box that fits the item and pile; tune `cbScaleYByLayer` against the actual per-layer height. Add optional settings such as `placeRemoveSound`, `upSolid`, or `maxFireable` only when appropriate, and verify their effect in the game.

  `modelItemsToStackSizeRatio` controls the relationship between model cube elements and item quantity during pile progression. For a model built from repeated item shapes, count all cube elements in one complete item, including nested children, and use that as an initial ratio. For example, the current insulator shape has 12 cubes per item and uses a ratio of `12`. This ratio is independent of stacking capacity: the model's total cube count does not, by itself, establish how many items will be visible. Confirm the displayed progression at several actual pile quantities in-game and adjust the ratio or model if needed.

3. **Create or reuse the pile model.** `stackingModel` points to a shape asset, separately from the item's normal held/inventory `shape`. For `signals:block/examplepile`, place the shape JSON at `assets/signals/shapes/block/examplepile.json`. Model the pile's appearance and height progression there, use valid texture references, and ensure element bounds fit the intended footprint and collision behavior. You do not normally need to create a blocktype or custom C# behavior for this built-in item behavior. Most of the time, the pile shape can be a simple collection of cubes with textures that match the item. If you want to use a different texture for the pile than the item, create a separate texture asset and reference it in the pile shape.

  **Replicating a multi-cube item shape:** When a pile should display a grid of repeated items, deep-copy every top-level element from the item's shape into each grid position, preserving each element's complete subtree, face definitions, UVs, and texture references. First determine how the shape format represents child coordinates (for example, parent-relative versus absolute) and apply placement transforms accordingly. In the current Signals insulator model, children are parent-relative, so each copied root receives a shared horizontal translation while child coordinates and child rotation origins remain unchanged. Translate the copied root's `from`, `to`, and `rotationOrigin` together; recursively suffix names (such as `_r1c1`) so names remain unique.

  Choose grid spacing based on the item's footprint and desired gaps, then choose starting offsets so the full grid fits the block model bounds. A general translation for a regular grid is:

  ```text
  dx = startX + column × spacingX
  dz = startZ + row × spacingZ
  ```

  `row` and `column` are zero-based grid indices; `startX`, `startZ`, `spacingX`, and `spacingZ` depend on the item's geometry, grid dimensions, and intended layout. The current insulator example uses a 4×4 grid, spacing of 4 shape units, and start offsets of -6 on both axes. It has three roots with three children apiece (12 cubes per item), yielding 48 roots and 192 total cubes in the pile model. These counts describe the shape JSON, not a guaranteed rendered item count: verify the configured ratio, full footprint (including nested geometry), and visual progression in-game.

4. **Account for variants.** A shared behavior/model is shared by all applicable item variants unless game-side configuration says otherwise. Check variant-specific shapes, textures, and other visual properties. Do not assume the pile model automatically inherits item-level variant textures; investigate supported type-specific stacking model/texture properties in installed itemtype examples or provide a suitable shared pile texture, then test each variant. For example, the current insulator item has raw and fired textures, but its pile shape directly references ceramic.

5. **Validate in context.** Keep the item’s regular `shape` and its pile's `stackingModel` paths distinct and correctly namespaced. Parse both shape JSON files and check that copied roots preserve the source subtrees and texture/face data, each translation is correct, names are unique, and the complete model bounds fit the intended footprint. Review the itemtype reference, then build/package the mod if relevant. A successful .NET build does not prove the game accepts the JSON or renders the pile correctly.

6. **Test in Vintage Story.** With each relevant variant, place one item and then additional items at representative quantities (including the first additions, model progression boundaries, and configured capacity). Confirm the pile appearance progresses as expected, transfers add/remove the intended quantities, capacity is respected, the pile empties and disappears correctly, and collision bounds are sensible at each layer. Check sounds and fire behavior too if configured. Verify variants with different visual properties individually. Fix asset or JSON errors reported in the game log.

## Project references

- [Insulator itemtype](../../../assets/signals/itemtypes/insulator.json) — `Stacking` configuration and raw/fired variants.
- [Insulator pile shape](../../../assets/signals/shapes/block/insulatorpile.json) — pile model with a fixed ceramic texture.
- [Insulator item shape](../../../assets/signals/shapes/item/insulator.json) — separate regular item model.
- [Wire itemtype](../../../assets/signals/itemtypes/el_wire.json) — `Quadrants` layout for comparison.