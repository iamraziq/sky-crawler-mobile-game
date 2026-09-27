# Skythread — Unity kit

Everything from the HTML prototype, ported to Unity: gameplay, difficulty ramp, all menus, save data, and every sprite.

## Fastest path (about 2 minutes)

1. **Create a project:** Unity Hub → New project → **2D (Built-in Render Pipeline)**. Unity 2021.3 LTS or newer; Unity 6 works. URP 2D also works.
2. **Import:** Assets → Import Package → Custom Package… → `Skythread.unitypackage` → Import all.
   *(Or copy the `Assets/Skythread` folder from this zip into your project's `Assets` folder.)*
3. **Build the scene:** menu **Tools → Skythread → Build Scene**.
   This sets the sprite import settings, creates the art library, builds the camera, game and every UI screen, saves `Assets/Skythread/Scenes/Skythread.unity`, adds it to Build Settings and sets portrait orientation.
4. **Play:** set the Game view to a portrait size (for example 1080×2160) and press Play.

Controls: tap or click anywhere to web-zip. On the main menu, the arrow buttons (or ←/→ keys) change hero. Space also zips; Esc or P pauses (Esc is the Android back button).

## What's inside

```
Assets/Skythread/
  Scripts/
    Game.cs          Gameplay: state machine, spawning, collisions, scrolling, FX (1:1 port of the prototype)
    UIManager.cs     Shows and hides screens, updates text, wires the buttons
    TapCatcher.cs    Full-screen tap area behind the menus → jump
    SkythreadArt.cs  ScriptableObject holding every sprite reference
  Editor/
    SkythreadBuilder.cs  Tools > Skythread > Build Scene (one-click setup)
  Sprites/           All game art as PNGs (4× resolution, 400 PPU)
  Fonts/             Russo One + JetBrains Mono (SIL Open Font License)
```

### Screens built for you
Main menu with a **hero picker** (big animated preview, previous/next arrows, name, tagline, page dots; the shown hero is auto-selected and saved) · HUD (score, saved counter, pause button, first-run hint) · Main menu · Pause (Resume with 3-2-1 countdown, Restart, Settings, Quit) · Game over (Retry, Main menu, new-best badge) · How to play · Settings (difficulty, rain toggle, reset best score with a confirm tap).

### Sprites
| File | Use |
|---|---|
| hero_<id>_wall / hero_<id>_air | 6 heroes (skythread, nightwire, solaris, frostbite, mantis, ember) on a wall / mid-jump. Flipped and rotated in code |
| web_splat | Web marks left on walls |
| rig | Window-washer platform |
| sign_bracket, sign_PIZZA/HOTEL/COLA/OPEN | Neon signs |
| laser_emitter, laser_beam, laser_node | Laser tripwire (beam is stretched) |
| drone_0…3 | Villain drone, rotor animation frames |
| mine_on / mine_off, warn | Falling mine, blink frames, "!" warning |
| civ_0…3, civ_glow, civ_ring | Collectible citizen (4 shirt colours) + pulsing halo |
| wall_tile, city_far | Tower walls and parallax city |
| fx_top, fx_bottom, fx_menu | Screen fades and the menu gradient |
| dot, pixel, rain | Particles, lines, rain streaks |
| ui_round, ui_circle, ui_chevron, ui_glow, icon_citizen | UI button shapes, picker arrows, hero glow and icon |

## Adding a hero
Add two PNGs `hero_<id>_wall.png` and `hero_<id>_air.png` to `Sprites/`, add a `Skin(...)` line in `SkythreadBuilder.CreateArt()` (or edit the **heroes** list on `SkythreadArt.asset` directly), and add one more dot in `BuildHeroPicker`.

## Tuning
All in `Game.cs`:
- `C()` sets base speed, acceleration, top speed and obstacle gap per difficulty.
- `Spawn()` sets when each hazard unlocks (distance thresholds 1600 / 4000 / 7000 = floors 80 / 200 / 350) and its odds.
- `nextGap` includes the extra room at the start (the `ease` factor).
- `JumpDur` is jump speed.

Coordinates are in the prototype's logical pixels: 390 wide, y pointing down. `P(x, y)` converts to world units (1 px = 0.01 units), so numbers can be compared 1:1 with the web version.

## Notes
- The UI uses Unity's built-in uGUI `Text`, so no TextMeshPro import is needed. If the fonts are missing, it falls back to Unity's default font.
- Both the old Input Manager and the new Input System are supported.
- No audio yet. Hooks to add it: `Jump()`, the citizen pickup in `Step()`, and `Die()`.
- Replace any PNG in `Sprites/` with final art using the same name and size, and the game picks it up.
