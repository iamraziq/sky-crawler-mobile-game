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

Tap/click anywhere to web-zip. On the main menu, the arrow buttons (or ←/→) change hero. Space, ↑ or Enter also zip; Esc or P pauses (Esc doubles as the Android back button).

## Getting started

1. **Unity version:** this project is set up with **Unity 6000.5.2f1** (URP 2D, Input System). The `Assets/Skythread` folder on its own also works in any Unity 2021.3 LTS+ 2D project, Built-in or URP.
2. **Open the project** in Unity Hub, or import `Assets/Skythread` into an existing 2D project.
3. **Build the scene:** menu **Tools → Skythread → Build Scene**. This sets sprite import settings, builds the art library, camera, game object and every UI screen, saves the scene, adds it to Build Settings, and sets portrait orientation.
4. **Play:** open `Assets/Skythread/Scenes/Skythread.unity`, set the Game view to a portrait size (e.g. 1080×2160) and press Play.

## Building for Android

1. Install the **Android Build Support** module (with SDK/NDK and OpenJDK) for your Unity version in Unity Hub.
2. **File → Build Profiles** → select **Android** → **Switch Platform**.
3. Make sure `Skythread.unity` is in the scene list, then **Build** (APK) or **Build And Run** with a device connected.

Current player settings: package `com.RaazStudios.SkyCrawler`, version `1.0` (build code 3), minimum Android 8.0 (API 26), portrait only.

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

## License

Code and original assets are released under the [MIT License](LICENSE).

Third-party assets keep their own licenses:
- **Fonts:** Russo One and JetBrains Mono, under the SIL Open Font License 1.1 (see `Assets/Skythread/Fonts/OFL-*.txt`).
