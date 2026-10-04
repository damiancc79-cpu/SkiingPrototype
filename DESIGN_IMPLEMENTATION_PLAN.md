# White Line — 3D Skiing Game Implementation Plan

## Product intent

**White Line** is a downhill skiing time-trial where raw speed is not enough. Players read changing snow, preserve momentum through carves, choose risky terrain, and decide whether to cash out at gates or push a line farther for score and Flow.

### Player fantasy

"Thread a fast, dangerous line through a living mountain, land one more trick, and make it to the next gate before the mountain takes it back."

## Core loop

1. Drop through a gate and build speed on the fall line.
2. Carve, brake, or use freshly packed grooves to manage momentum.
3. Read weather and terrain: low visibility, crosswind, bumps, obstacles, route splits.
4. Build **Flow** through high speed, air time, close passes, and clean tricks.
5. Spend Flow on Focus boost to hold a faster line; score remains unbanked until a gate.
6. Crash and lose the current line; clear gates to protect score and race for time.

## Systems and implementation order

| Phase | System | Player-facing outcome | Implementation / acceptance criteria |
| --- | --- | --- | --- |
| 1 | Momentum ski model | Speed comes from slope, and carving/braking trades speed for control. | Rigidbody skier applies downhill force, lateral carving, friction, air control, landing and collision crashes. A complete run is possible with keyboard or gamepad. |
| 2 | Course grammar | The mountain creates decisions rather than a straight corridor. | Ordered gates, route-choice islands, bumps, jump, technical pole section, finish catch and reset point generate at runtime. |
| 3 | Deformable snow | Repeated ski passes visibly pack snow and change how a line feels. | `SnowSurfaceSystem` stamps two tracks into a sparse grid, samples compaction, lowers drag on hardpack, and reduces carving authority. Tracks slowly recover. |
| 4 | Weather | Conditions modify reading and control, not only the skybox. | `WeatherDirector` supports bluebird, snowfall, whiteout and crosswind. It changes fog, snow density and applies a physically meaningful lateral wind force. F cycles conditions for testing. |
| 5 | Tricks | Air time has an active decision with a clear failure state. | Q/E starts left/right 360s in air; W adds a grab bonus. Clean landings score, unfinished rotations crash. Shoulder and south gamepad controls mirror this. |
| 6 | Risk/reward | Players decide when to bank and when to overextend. | Speed, carving, air and near misses fill Flow. Gates bank points; crashes discard unbanked score. Left Shift spends Flow for Focus acceleration. |
| 7 | Feedback & tuning | Game state stays legible at speed. | HUD reports condition, snow, Flow, score and trick state. Existing spray, trails, camera feedback and audio respond to movement. Tune values through serialized fields. |

## First-playable control map

- A/D or Left/Right: carve
- Space, S, or Down: brake/skid
- Q/E: left/right 360 while airborne
- W: grab modifier during a spin
- Left Shift: Focus boost (spends Flow)
- F: cycle weather (prototype debug control)
- R: restart the run

## Architecture

`SkiPrototypeBootstrap` creates the graybox course and wires gameplay systems together. Systems communicate through narrow public APIs/events rather than scene-specific object names:

```text
SkiInputSource ──> RigidbodySkierController ──> SkierCrashHandler
                                      │                    │
SnowSurfaceSystem ───────────────────┘                    │
WeatherDirector ──> Rigidbody force                       │
TrickSystem ─────────> trick landing/failure ─────────────┤
RaceManager <── gates ──> RiskRewardSystem <──────────────┘
                     └──> RaceHud
```

## Validation checklist

- [x] Speed and steering respond to slope, carving, braking and air state.
- [x] Snow tracks accumulate and alter the friction/grip response sampled under skis.
- [x] Weather changes visibility and adds wind force where relevant.
- [x] A 360 can be completed and rewarded; an unfinished aerial move crashes.
- [x] Gates bank score, while crashes clear the active line.
- [x] HUD exposes all live decision-making signals.
- [ ] Playtest and tune difficulty using the intended Unity version (`6000.4.10f1`).

## Next production milestones

1. Replace graybox terrain with chunked heightfields and GPU snow displacement/normal masks.
2. Add authored terrain tags (ice, powder, slush, avalanche zones) and a forecast-driven run selector.
3. Move to animation/IK skiing poses, trick state machine, replay camera and ghost races.
4. Add progression through mountain contracts: time, style, survival and weather objectives.
5. Profile pooled snow stamps/particles, then migrate dense deformation to instanced decals or a render texture.
