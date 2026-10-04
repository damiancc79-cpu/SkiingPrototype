# White Line — Graybox Skiing Time Trial Prototype

Open this folder with Unity Hub, then open `Assets/SkiingPrototype/Scenes/DownhillPrototype.unity` and press Play.

Controls:
- `A/D` or left/right arrows: carve
- `S`, down arrow, or `Space`: brake/skid
- `Q` / `E` while airborne: spin left / right 360
- `W` during a spin: grab for a score bonus
- `Left Shift`: Focus boost while Flow is available
- `F`: cycle prototype weather conditions
- `R`: reset to the start point

The scene is intentionally built at runtime by `SkiPrototypeBootstrap`, so the checked-in scene stays tiny while the prototype remains easy to regenerate and tune. Press Play to build a complete downhill time-trial loop with a start gate, six ordered checkpoint gates, a finish gate, jumps, route-choice sections, a tighter technical section, and primitive graybox obstacles.

This pass also adds arcade feedback around the existing physics controller:
- Speed- and skid-responsive snow spray.
- Landing and crash snow bursts.
- Lightweight ski trails that thicken during carving/skidding.
- Procedural visual lean, ski yaw, and landing compression.
- Camera turn lag, speed FOV, landing response, and crash pullback.
- Runtime-generated placeholder audio for snow, skid, landing, crash, and checkpoint feedback.
- Packed snow grooves that make previously ridden lines faster but reduce carving authority.
- Snowfall, whiteout and crosswind states that change visibility and movement.
- Trick, Flow and gate-banked score systems that make risky lines worthwhile.

For the full game design and Codex-ready build sequence, see [DESIGN_IMPLEMENTATION_PLAN.md](DESIGN_IMPLEMENTATION_PLAN.md).

Core scripts:
- `SkiInputSource`: legacy input wrapper.
- `SlopeProbe`: spherecast ground detection, terrain normal, and slope angle.
- `RigidbodySkierController`: slope-projected movement, downhill acceleration, carving, braking, friction, air control, landing detection, and reset.
- `SkierCrashHandler`: high-impact and bad-landing crash logic.
- `SkiFollowCamera`: third-person follow camera with speed-based FOV.
- `SkiDebugOverlay`: IMGUI debug readout for speed, grounded state, slope angle, and movement state.
- `RaceGate`: start/checkpoint/finish trigger and gate visual state.
- `RaceManager`: run state machine, ordered checkpoint flow, timer, restart, crash integration, and local best time via `PlayerPrefs`.
- `RaceHud`: gameplay HUD and finish results panel.
- `SkierFeedbackController`: particles, trails, visual lean/compression, camera feedback, and audio placeholders.
