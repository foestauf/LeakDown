# LeakDown Backlog

Findings and ideas from the 2026-06-11 code review that were deliberately **not**
shipped in v0.3.2. Roughly ordered by impact within each section.

## Features

### Catch-up leakdown for unsimulated cars
Derail Valley unloads distant cars, so a loco parked overnight loses no pressure
while it isn't being simulated. Apply elapsed-time decay when a car re-enters
simulation (compute `P1 = P0 * exp(-k * elapsedInGameTime)` on resume). This is
the biggest remaining realism gap.

### Non-locomotive brake leakage
Parked cars bleeding off brake pressure over time (with the runaway-risk gameplay
that implies).

### Player feedback cues
A faint hiss sound or gauge flutter so players can tell a worn loco is leaking,
rather than discovering it from the manual. Scale intensity with the wear
multiplier.

## Verification

### In-game test of the v0.3.2 wear fix
The enum mapping fix is compile-verified against the game assembly but has not
been tested in-game. Spawn a restoration loco at various states (S0 wreck,
S3 rerailed, S9 serviced) and confirm the DEBUG log shows the expected
multipliers (2.5x / 1.5x / 0.8x).

## Tech debt / polish

- **Clamp settings on load** — values from a hand-edited `Settings.xml` aren't
  validated; clamp both sliders to 0–500 in `Settings` load/draw.
- **Dead `[Draw]` attributes** — `Settings` implements a custom `Draw()`, so the
  UMM `[Draw("...")]` attributes are never used. Remove them, or drop the custom
  method and let UMM auto-draw.
- **`Main.TimeScale` null guard** — `Globals.G.GameParams` could be null outside
  gameplay; probably unreachable from the patches, but a cheap guard.
- **Replace `vessel` reflection with publicized access** — Krafs.Publicizer
  already publicizes `Assembly-CSharp`; `BoilerExtensions.VesselField` reflection
  may be replaceable with direct field access. Verify `Boiler.vessel` lives in a
  publicized assembly first.
- **Build setup not self-contained** — the csproj depends on
  `../Directory.Build.Targets` which lives outside this repo. Commit an example
  file (or the real one) and document the `DerailValleyPath` property in the
  README so fresh clones build.
- **`info.json` `Repository` field** — add a repository URL so UMM can offer
  update checks.
- **Tidy `IMPLEMENTATION_NOTES.md`** — the "After (Simple)" example shows the
  `GetComponentInParent` approach that the same document says failed; the notes
  predate the final SimController architecture. Rewrite or trim to match
  what shipped.
