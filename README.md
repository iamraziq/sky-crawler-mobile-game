# Sky Crawler

Web-zip between skyscrapers, dodge Dr. Volt's traps, and see how far you can push your run.

Sky Crawler is a one-tap endless runner built in Unity: swing between skyscraper walls, dodge escalating hazards, and chase your best score across six playable heroes.

## Gameplay

- **Tap to zip** — one tap, one motion between skyscraper walls
- **Escalating hazards** — window-washer rigs, neon signs, laser tripwires, drones, and falling mines unlock as you climb higher
- **Rescue citizens** — grab stranded citizens mid-run for bonus score
- **Six heroes** — Skythread, Nightwire, Solaris, Frostbite, Mantis, and Ember Kid, each with their own look
- **Three difficulty modes** — Chill, Normal, Hard
- **Rain toggle** — set the mood in Settings
- **Best score tracking** — saved locally between runs

## Controls

Tap/click anywhere to web-zip. On the main menu, the arrow buttons (or ←/→) change hero. Space also zips; Esc or P pauses (Esc doubles as the Android back button).

## Getting started

1. **Unity version:** 2021.3 LTS or newer (Unity 6 works). 2D Built-in Render Pipeline or URP 2D both work.
2. **Open the project** in Unity Hub, or import `Assets/Skythread` into an existing 2D project.
3. **Build the scene:** menu **Tools → Skythread → Build Scene**. This sets sprite import settings, builds the art library, camera, game object and every UI screen, saves the scene, adds it to Build Settings, and sets portrait orientation.
4. **Play:** set the Game view to a portrait size (e.g. 1080×2160) and press Play.

## Project structure

```
Assets/Skythread/
  Scripts/
    Game.cs          Gameplay: state machine, spawning, collisions, scrolling, FX
    UIManager.cs      Shows/hides screens, updates text, wires buttons
    AudioManager.cs   Background music + one-shot SFX for every gameplay/UI event
    TapCatcher.cs     Full-screen tap area behind the menus → jump
    SkythreadArt.cs   ScriptableObject holding every sprite reference
  Editor/
    SkythreadBuilder.cs   Tools > Skythread > Build Scene (one-click setup)
  Sprites/            Game art (PNGs, 4× resolution, 400 PPU)
  Fonts/              Russo One + JetBrains Mono (SIL Open Font License)
```

## Tuning

All in `Game.cs`:
- `C()` — base speed, acceleration, top speed, obstacle gap per difficulty
- `Spawn()` — distance thresholds and odds for each hazard type
- `JumpDur` — jump speed

Coordinates are in logical pixels (390 wide, y pointing down); `P(x, y)` converts to world units.

## Status

- [x] Core gameplay, menus, and save data
- [x] Audio (music + SFX hooks across jump, pickups, death, countdown, pause, UI clicks)
- [x] Exit-to-desktop button
- [ ] _add anything else you're working on_

## License

_Add a license here before making the repo public if you haven't already (e.g. MIT, or "All rights reserved" if you don't want reuse)._
