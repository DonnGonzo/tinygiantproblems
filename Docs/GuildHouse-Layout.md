# Guild House layout (rebuilt from zero, November 2026)

The old Guild House (two imported meshes, `guild-house` and
`guild-house-interior`, cut and patched at runtime, plus terrain levelling)
was deleted. The new house is built at runtime by
`Scripts/Gameplay/GuildHall.cs` out of a small set of modular, box-collided
pieces. It uses one shared material palette (`GuildHall.Palette`). Every
measurement below is in metres, in the house's local frame:

- `+Z` points out of the front door, towards Mossback.
- `+X` is to the right as you walk in.
- `y = 0` is the top of the indoor floor.

The technician is 1.65 m tall. Their jump clears about 1.3 m, and their
CharacterController steps up 0.35 m. Every route below follows those limits:

- Stair risers are 0.25 m.
- Jump-route rises are 0.9 to 1.0 m.
- Jump gaps are 0.4 m or less, except one sprint gap.

## Concept

This is the Small Works Guild's hall, seen from the tiny technician's point of
view. It is a tall, two-storey timber-and-plaster hall with a gallery running
round three walls, a loft over the middle and a balcony high on the right
gable. The ground floor is for business. The upper levels are a playground
that teaches the jump, the claw and the glider before you use them on
Mossback.

## Shell

| Piece | Size / position |
|---|---|
| Footprint (outside of walls) | x -11 to 11, z -8 to 8 (22 x 16) |
| Walls | 0.5 thick, eaves at y 9. Gables at x = ±11 rise to the ridge at y 13 (z = 0) |
| Foundation | Stone, overhangs 0.4. Runs from y 0 down to below the lowest ground under the house, so no side ever floats |
| Roof | Two shingle slabs at 26.6°, 0.8 eave overhang, ridge cap, back chimney |
| Front door | Open doorway x -1.7 to 1.7, 4.2 high. Porch canopy, "GUILD HOUSE" sign |
| Front steps | Generated from the threshold down to the measured meadow height |

## Ground floor (business)

```
            back wall (fireplace at centre)
  +--------------------------------------------+
  | WARDROBE   [fire]         shelf tower      |
  | (secret gap                 s1..s4  ->     |
  |  behind it)    +-------------+             |
  |                |    LOFT     |             |
  | TOOL           | (overhead)  |       SELL  |
  | LOCKER         |=CLIMB WALL==|    COUNTER  |
  |                                            |
  | crate climb ->     ( rug )        stairs   |
  | barrel/crate/shelf              ->->->->   |
  +-----------------[  door  ]-----------------+
                 front, towards Mossback
```

| Station | Where |
|---|---|
| Tool Locker (shop) | Left wall, centre (x -8.4). Tool case on a long counter, pegboard of tools |
| Sell counter | Right wall, centre (x 8.4). Salvage chest whose lid bounces when you sell, and a set of scales |
| Wardrobe (colours) | Back-left (x -5, z -6). A small gap behind it leads to a secret alcove |
| Notice board | Just inside the door on the left (decor that ties into the mission board) |
| Fireplace | Back wall centre. Warm light, a landmark you can see from the door |
| Rug | Round red rug in the middle. You can see it from the door and from every height |
| Winch Post | Outside, 3.5 m in front of the door and 5.5 m to the right (free ride up to Mossback's back) |

## Upper levels (traversal)

| Level | Height | Area |
|---|---|---|
| Gallery | 4.0 | Back strip z -7.5 to -4.5 plus side strips x ±7.5 to ±10.5. Railings on the inner edges, with gaps where routes arrive |
| Loft | 8.0 | x -4 to 4, z -3.5 to 1.5, on four posts. Telescope, chest, railing at the back and left |
| High beam | 8.0 | 0.8 wide, from the loft's right edge out to the right-gable door |
| Gable balcony | 8.0 | Outside the right gable, x 11 to 13.2. Open end for a glider launch |

### Routes

1. **Stairs (always available).** Run along the inside of the front wall, from
   x 2.6 up to x 7.5. That is 16 risers of 0.25 m, ending on the right gallery.
2. **Crate climb (jumping).** Barrel at 1.0, then crate at 2.0, then shelf at
   3.0, then the left gallery at 4.0. Each landing edge has a guild-gold trim.
3. **Shelf tower (jumping, back right).** From the back gallery:
   - s1 at 4.9, s2 at 5.8, s3 at 6.7, s4 at 7.6.
   - Then a 0.4 m hop onto the high beam at 8.0.
   - Walk the beam left to the loft or right out of the gable door.
   - No shelf is above another one in plan view, so there are no head bumps.
4. **Climbing wall (claw).** A 6 m wide, 8 m tall bouldering wall forms the
   front face of the loft and faces the door. With the Climbing Claw you climb
   straight from the hall floor up to the loft. It counts toward "Claw-climb"
   objectives (8 m per climb).
5. **Glide off the balcony.** About 8 m of drop plus the meadow slope give a
   long glide towards the yard ("Glide for 5 seconds").
6. **Roof.** The 26.6° roof is walkable. From the balcony you can reach the
   gable overhang with the glider or by jumping, as a hidden bonus.

### Hidden spots (trinkets, respawned every mission)

| Spot | Find |
|---|---|
| Under the stairs, behind crates with a 0.9 m gap | Shiny Pebble |
| Secret alcove behind the wardrobe | Shiny Pebble |
| End of the gable balcony | Lost Guild Badge |

These count toward "Find hidden trinkets" and the "Find every hidden
trinket" challenge, so the house's traversal also feeds missions 2 and 6.

## Readability rules

- Walkable tops are light planks. Walls are cream plaster. Structure is dark
  timber.
- Every landing you jump to has a guild-gold edge trim.
- The climbable wall is the only blue-grey surface, studded with coloured
  holds.
- Railings mark edges you can't cross. A gap in a railing means a route
  continues there.
- Point lights mark each station and the routes. There are no shadows, so
  they stay cheap.

## Performance

The static structure is static-batched at build time. All pieces share about
a dozen palette materials. The lights don't cast shadows. Colliders are plain
boxes, with no mesh colliders.

Baked lighting isn't used, because the house is built at runtime (so it works
in the existing scene without hand-editing). The warm point lights and the
ambient light from the scene do that job.
