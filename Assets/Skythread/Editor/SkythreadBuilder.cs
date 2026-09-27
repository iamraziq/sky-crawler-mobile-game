using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Skythread.EditorTools
{
    /// <summary>
    /// One-click setup: Tools > Skythread > Build Scene.
    /// Configures sprite import settings, creates the art library, builds the full scene
    /// (camera, game, all UI screens) and saves it to Assets/Skythread/Scenes/Skythread.unity.
    /// Safe to run again; it rebuilds the scene from scratch.
    /// </summary>
    public static class SkythreadBuilder
    {
        const string Root = "Assets/Skythread";
        const float Q = 10.8f; // 1% of screen width in reference pixels (1080 wide) — same as "cqw" in the prototype

        static Font display, mono, monoBold;
        static Sprite round, circle, citizenIcon, menuGrad, chevron, glowSprite;

        static Color C(string h, float a = 1f) { Color c; ColorUtility.TryParseHtmlString(h, out c); c.a = a; return c; }
        static readonly Color Red = C("#e8283c"), RedDark = C("#a3121f"), Blue = C("#3d7bff"), Ink = C("#f5f8ff"),
            Muted = C("#b4c2e0"), Soft = C("#dde5f5"), Navy = C("#0a0f1f"), PanelBg = C("#0b1122"), Pink = C("#ff7a90"),
            Danger = C("#ff5a6a"), Gold = C("#ffe99a");

        enum Kind { Primary, Secondary, Ghost }

        [MenuItem("Tools/Skythread/Build Scene")]
        public static void Build()
        {
            ConfigureTextures();
            var art = CreateArt();
            LoadUiAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 3.9f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Navy;
            cam.transform.position = new Vector3(0, 0, -10);

            var game = new GameObject("Game").AddComponent<Game>();
            game.art = art; game.cam = cam;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>().sendNavigationEvents = false;
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
            game.ui = BuildUI(game);

            Directory.CreateDirectory(Root + "/Scenes");
            string scenePath = Root + "/Scenes/Skythread.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            AssetDatabase.SaveAssets();
            Debug.Log("Skythread: scene built at " + scenePath + ". Set the Game view to a portrait resolution (e.g. 1080x2160) and press Play.");
        }

        // ---------------- assets ----------------
        static void ConfigureTextures()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/Sprites" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                string n = Path.GetFileNameWithoutExtension(path);
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = (n.StartsWith("fx_") || n.StartsWith("ui_") || n.StartsWith("icon_")) ? 100 : n == "city_far" ? 200 : 400;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.filterMode = FilterMode.Bilinear;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.maxTextureSize = 4096;
                ti.spriteBorder = n == "ui_round" ? new Vector4(20, 20, 20, 20) : Vector4.zero;
                ti.SaveAndReimport();
            }
        }

        static Sprite S(string n)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Sprites/" + n + ".png");
            if (!s) Debug.LogWarning("Skythread: missing sprite " + n);
            return s;
        }

        static SkythreadArt CreateArt()
        {
            string path = Root + "/SkythreadArt.asset";
            var art = AssetDatabase.LoadAssetAtPath<SkythreadArt>(path);
            if (!art) { art = ScriptableObject.CreateInstance<SkythreadArt>(); AssetDatabase.CreateAsset(art, path); }
            art.heroes = new[]
            {
                Skin("skythread", "Skythread", "The original web-zipper", "#e8283c"),
                Skin("nightwire", "Nightwire", "Sees in the dark", "#1c1b2a"),
                Skin("solaris", "Solaris", "Sunrise on the skyline", "#ffb020"),
                Skin("frostbite", "Frostbite", "Cool under pressure", "#bfe9ff"),
                Skin("mantis", "Mantis", "Strikes from the shadows", "#34c46e"),
                Skin("ember", "Ember Kid", "Keeps the city warm", "#ff6a2b"),
            };
            art.webSplat = S("web_splat");
            art.rig = S("rig"); art.signBracket = S("sign_bracket");
            art.signs = new[] { S("sign_PIZZA"), S("sign_HOTEL"), S("sign_COLA"), S("sign_OPEN") };
            art.laserEmitter = S("laser_emitter"); art.laserBeam = S("laser_beam"); art.laserNode = S("laser_node");
            art.drone = new[] { S("drone_0"), S("drone_1"), S("drone_2"), S("drone_3") };
            art.mineOn = S("mine_on"); art.mineOff = S("mine_off"); art.warn = S("warn");
            art.civ = new[] { S("civ_0"), S("civ_1"), S("civ_2"), S("civ_3") };
            art.civGlow = S("civ_glow"); art.civRing = S("civ_ring");
            art.wallTile = S("wall_tile"); art.cityFar = S("city_far");
            art.fadeTop = S("fx_top"); art.glowBottom = S("fx_bottom");
            art.dot = S("dot"); art.pixel = S("pixel"); art.rain = S("rain");
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            return art;
        }

        static HeroSkin Skin(string id, string name, string tag, string hex)
        {
            var main = C(hex);
            if (id == "nightwire") main = C("#9b6bff"); // burst colour for the dark suit
            return new HeroSkin { id = id, name = name, tagline = tag, main = main, wall = S("hero_" + id + "_wall"), air = S("hero_" + id + "_air") };
        }

        static void LoadUiAssets()
        {
            display = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/RussoOne-Regular.ttf");
            mono = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/JetBrainsMono-Regular.ttf");
            monoBold = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/JetBrainsMono-SemiBold.ttf");
            Font fallback = null;
            try { fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (!fallback) { try { fallback = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            if (!display) display = fallback;
            if (!mono) mono = fallback;
            if (!monoBold) monoBold = mono;
            round = S("ui_round"); circle = S("ui_circle"); citizenIcon = S("icon_citizen"); menuGrad = S("fx_menu"); chevron = S("ui_chevron"); glowSprite = S("ui_glow");
        }

        // ---------------- UI helpers ----------------
        static RectTransform RT(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero; }
        static void Anchor(RectTransform r, float ax, float ay, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(ax, ay); r.pivot = new Vector2(ax, ay);
            r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h);
        }
        static Image Img(RectTransform r, Sprite s, Color c, bool sliced = false, bool raycast = true, float ppuMul = 0.75f)
        {
            var i = r.gameObject.AddComponent<Image>(); i.sprite = s; i.color = c; i.raycastTarget = raycast;
            if (sliced) { i.type = Image.Type.Sliced; i.pixelsPerUnitMultiplier = ppuMul; }
            return i;
        }
        static Text Txt(Transform parent, string name, string s, Font f, float cq, Color c, TextAnchor a = TextAnchor.MiddleCenter, float hCq = 0)
        {
            var r = RT(name, parent);
            var t = r.gameObject.AddComponent<Text>();
            t.font = f; t.text = s; t.fontSize = Mathf.RoundToInt(cq * Q); t.color = c; t.alignment = a;
            t.supportRichText = true; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            var le = r.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = hCq > 0 ? hCq * Q : cq * Q * 1.35f;
            return t;
        }
        static Button Btn(Transform parent, string name, string label, Kind k, float hCq, float fontCq)
        {
            var r = RT(name, parent);
            var bg = k == Kind.Primary ? Red : k == Kind.Secondary ? C("#3d7bff", 0.12f) : new Color(0, 0, 0, 0);
            var img = Img(r, round, bg, true);
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = img;
            var cb = b.colors; cb.highlightedColor = Color.white; cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1); cb.selectedColor = Color.white; b.colors = cb;
            if (k == Kind.Primary) { var sh = r.gameObject.AddComponent<Shadow>(); sh.effectColor = RedDark; sh.effectDistance = new Vector2(0, -Q); }
            if (k == Kind.Secondary) { var ol = r.gameObject.AddComponent<Outline>(); ol.effectColor = C("#3d7bff", 0.6f); ol.effectDistance = new Vector2(4, 4); }
            var le = r.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = hCq * Q; le.minHeight = hCq * Q;
            var t = Txt(r, "Label", label, k == Kind.Primary ? display : monoBold, fontCq, k == Kind.Primary ? Color.white : k == Kind.Ghost ? Pink : Ink);
            Stretch(t.rectTransform);
            Object.DestroyImmediate(t.GetComponent<LayoutElement>());
            return b;
        }
        static VerticalLayoutGroup VL(RectTransform r, float spacingCq, float padX, float padTop, float padBottom, TextAnchor a = TextAnchor.MiddleCenter)
        {
            var v = r.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacingCq * Q;
            v.padding = new RectOffset((int)(padX * Q), (int)(padX * Q), (int)(padTop * Q), (int)(padBottom * Q));
            v.childAlignment = a;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }
        static HorizontalLayoutGroup HL(RectTransform r, float spacingCq, bool expandWidth)
        {
            var h = r.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacingCq * Q; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = expandWidth; h.childForceExpandHeight = true;
            return h;
        }
        static void Spacer(Transform p) { RT("Spacer", p).gameObject.AddComponent<LayoutElement>().flexibleHeight = 1; }
        static void Gap(Transform p, float cq) { RT("Gap", p).gameObject.AddComponent<LayoutElement>().preferredHeight = cq * Q; }
        static RectTransform Row(Transform p, string name, float hCq) { var r = RT(name, p); r.gameObject.AddComponent<LayoutElement>().preferredHeight = hCq * Q; return r; }
        static RectTransform Panel(Transform parent, string name, Color bg, Sprite s = null)
        {
            var r = RT(name, parent); Stretch(r); Img(r, s, bg); return r;
        }
        static void Divider(Transform p) { var l = RT("Divider", p); l.gameObject.AddComponent<LayoutElement>().preferredHeight = 3; Img(l, null, C("#3d7bff", 0.2f), false, false); }
        static RectTransform Pill(Transform parent, string name, Color c) { var r = RT(name, parent); Img(r, round, c, true, false, 0.3f); return r; }

        // ---------------- UI ----------------
        static UIManager BuildUI(Game game)
        {
            var cgo = new GameObject("UI", typeof(RectTransform));
            var canvas = cgo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = cgo.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1080, 2160); sc.matchWidthOrHeight = 0;
            cgo.AddComponent<GraphicRaycaster>();
            var ui = cgo.AddComponent<UIManager>();
            ui.game = game;
            var root = cgo.transform;

            // Tap anywhere to jump
            var tap = RT("TapCatcher", root); Stretch(tap); Img(tap, null, new Color(0, 0, 0, 0));
            tap.gameObject.AddComponent<TapCatcher>().game = game;

            // ---- HUD ----
            var hud = RT("HUD", root); Stretch(hud); ui.hud = hud.gameObject;
            ui.score = Txt(hud, "Score", "0", display, 12, Ink); Anchor(ui.score.rectTransform, 0.5f, 1, 0, -8 * Q, 80 * Q, 14 * Q);
            var pill = Pill(hud, "Saved", C("#050912", 0.65f)); Anchor(pill, 1, 1, -14 * Q, -9 * Q, 17 * Q, 7 * Q);
            var ic = RT("Icon", pill); Anchor(ic, 0, 0.5f, 2.6f * Q, 0, 3.4f * Q, 3.4f * Q); Img(ic, citizenIcon, Color.white, false, false);
            ui.coins = Txt(pill, "Count", "0", monoBold, 3.8f, Gold, TextAnchor.MiddleLeft); Anchor(ui.coins.rectTransform, 0, 0.5f, 7.6f * Q, 0, 9 * Q, 6 * Q);
            var pb = RT("Pause", hud); Anchor(pb, 0, 1, 14 * Q, -8 * Q, 11 * Q, 11 * Q);
            var pbi = Img(pb, circle, C("#050912", 0.65f));
            ui.pauseBtn = pb.gameObject.AddComponent<Button>(); ui.pauseBtn.targetGraphic = pbi;
            for (int i = 0; i < 2; i++)
            {
                var bar = RT("Bar", pb); Anchor(bar, 0.5f, 0.5f, (i == 0 ? -1.4f : 1.4f) * Q, 0, 1.4f * Q, 4 * Q);
                Img(bar, null, Ink, false, false);
            }
            var hint = Pill(hud, "Hint", C("#050912", 0.65f)); Anchor(hint, 0.5f, 0, 0, 12 * Q, 52 * Q, 8 * Q); ui.hint = hint.gameObject;
            Stretch(Txt(hint, "Label", "TAP TO WEB-ZIP", monoBold, 3.6f, Ink).rectTransform);

            // ---- Main menu ----
            var menu = Panel(root, "MainMenu", Color.white, menuGrad); ui.menu = menu.gameObject;
            VL(menu, 3, 12, 18, 12);
            Txt(menu, "Eyebrow", "NIGHT SHIFT", monoBold, 3, Blue);
            Txt(menu, "Title", "SKY<color=#e8283c>THREAD</color>", display, 12.5f, Ink).horizontalOverflow = HorizontalWrapMode.Overflow;
            Txt(menu, "Tagline", "THE CITY NEEDS A HERO", mono, 3.2f, Muted);
            Spacer(menu);
            BuildHeroPicker(menu, ui);
            Spacer(menu);
            ui.menuBest = Txt(menu, "Best", "BEST 0 FLOORS", mono, 3.2f, Muted);
            ui.playBtn = Btn(menu, "Play", "SUIT UP", Kind.Primary, 14, 7);
            var mrow = Row(menu, "Row", 11); HL(mrow, 3, true);
            ui.howBtn = Btn(mrow, "HowToPlay", "HOW TO PLAY", Kind.Secondary, 11, 3.4f);
            ui.settingsBtn = Btn(mrow, "Settings", "SETTINGS", Kind.Secondary, 11, 3.4f);

            // ---- Game over ----
            var over = Panel(root, "GameOver", C("#050912", 0.78f)); ui.over = over.gameObject;
            VL(over, 4, 12, 10, 10);
            Spacer(over);
            Txt(over, "Title", "WEB SNAPPED", display, 10, Danger);
            ui.overReason = Txt(over, "Reason", "", monoBold, 4.4f, Ink, TextAnchor.MiddleCenter, 13);
            ui.overScore = Txt(over, "Score", "0", display, 20, Ink, TextAnchor.MiddleCenter, 22);
            var nbRow = Row(over, "NewBest", 6); ui.overNewBest = nbRow.gameObject;
            var nb = Pill(nbRow, "Pill", Red); Anchor(nb, 0.5f, 0.5f, 0, 0, 30 * Q, 5.5f * Q);
            Stretch(Txt(nb, "Label", "NEW BEST", monoBold, 3.2f, Color.white).rectTransform);
            var stats = Row(over, "Stats", 6); HL(stats, 6, true);
            ui.overBest = Txt(stats, "Best", "BEST 0", mono, 3.4f, Soft, TextAnchor.MiddleRight);
            ui.overSaved = Txt(stats, "Saved", "SAVED 0", mono, 3.4f, Soft, TextAnchor.MiddleLeft);
            var ob = Row(over, "Buttons", 29); VL(ob, 3, 0, 3, 0); ui.overButtons = ob.gameObject;
            ui.retryBtn = Btn(ob, "Retry", "BACK ON PATROL", Kind.Primary, 13, 6);
            ui.overMenuBtn = Btn(ob, "Menu", "MAIN MENU", Kind.Secondary, 11, 3.6f);
            Spacer(over);

            // ---- Pause ----
            var pause = Panel(root, "Pause", C("#050912", 0.86f)); ui.pause = pause.gameObject;
            VL(pause, 3, 14, 10, 10);
            Spacer(pause);
            Txt(pause, "Title", "PAUSED", display, 12, Ink);
            ui.pauseInfo = Txt(pause, "Info", "", mono, 3.4f, Muted);
            Gap(pause, 4);
            ui.resumeBtn = Btn(pause, "Resume", "RESUME", Kind.Primary, 13, 6);
            ui.restartBtn = Btn(pause, "Restart", "RESTART", Kind.Secondary, 11, 3.6f);
            ui.pauseSettingsBtn = Btn(pause, "Settings", "SETTINGS", Kind.Secondary, 11, 3.6f);
            ui.quitBtn = Btn(pause, "Quit", "QUIT TO MENU", Kind.Ghost, 11, 3.6f);
            Spacer(pause);

            // ---- Countdown ----
            var cdp = RT("Countdown", root); Stretch(cdp); ui.countdown = cdp.gameObject;
            ui.countdownText = Txt(cdp, "Number", "3", display, 30, Red); Stretch(ui.countdownText.rectTransform);
            ui.countdownText.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -Q);

            // ---- How to play ----
            var how = Panel(root, "HowToPlay", PanelBg); ui.how = how.gameObject;
            VL(how, 4, 10, 12, 10, TextAnchor.UpperLeft);
            Txt(how, "Title", "How to play", display, 8.5f, Ink, TextAnchor.MiddleLeft, 12);
            HowStep(how, "1", "Tap anywhere to web-zip to the other tower.", false);
            HowStep(how, "2", "Touch lasers, drones or anything else and your web snaps.", false);
            HowStep(how, "", "Catch citizens drifting down on glowing umbrellas.", true);
            Gap(how, 2);
            Txt(how, "HazardsLabel", "HAZARDS", mono, 3, Muted, TextAnchor.MiddleLeft);
            Hazard(how, "Washer rigs, neon signs", "FL 0");
            Hazard(how, "Laser tripwires", "FL 80");
            Hazard(how, "Volt drones", "FL 200");
            Hazard(how, "Falling mines \u2014 watch for <color=#e8283c>!</color>", "FL 350");
            Txt(how, "Keys", "Keyboard: Space to zip \u00b7 Esc to pause", mono, 3, Muted, TextAnchor.MiddleLeft);
            Spacer(how);
            ui.howBackBtn = Btn(how, "GotIt", "GOT IT", Kind.Primary, 12, 3.8f);

            // ---- Settings ----
            var set = Panel(root, "Settings", PanelBg); ui.settings = set.gameObject;
            VL(set, 5, 10, 12, 10, TextAnchor.UpperLeft);
            Txt(set, "Title", "Settings", display, 8.5f, Ink, TextAnchor.MiddleLeft, 12);
            Txt(set, "DifficultyLabel", "DIFFICULTY", mono, 3, Muted, TextAnchor.MiddleLeft);
            var seg = Row(set, "Difficulty", 11); Img(seg, round, C("#3d7bff", 0.1f), true, false);
            var sh = HL(seg, 1, true); sh.padding = new RectOffset((int)Q, (int)Q, (int)Q, (int)Q);
            ui.chillBtn = Btn(seg, "Chill", "CHILL", Kind.Ghost, 9, 3.4f);
            ui.normalBtn = Btn(seg, "Normal", "NORMAL", Kind.Ghost, 9, 3.4f);
            ui.hardBtn = Btn(seg, "Hard", "HARD", Kind.Ghost, 9, 3.4f);
            ui.rainBtn = RowButton(set, "Rain", "City rain", out ui.rainValue, out _);
            ui.resetBtn = RowButton(set, "ResetBest", "Best \u00b7 floor 0", out ui.resetValue, out ui.settingsBest);
            ui.resetValue.color = Pink;
            Spacer(set);
            ui.settingsDoneBtn = Btn(set, "Done", "DONE", Kind.Primary, 12, 3.8f);

            return ui;
        }

        static Button CircleButton(Transform parent, string name, bool flip)
        {
            var r = RT(name, parent);
            var img = Img(r, circle, C("#3d7bff", 0.14f));
            var ol = r.gameObject.AddComponent<Outline>(); ol.effectColor = C("#3d7bff", 0.6f); ol.effectDistance = new Vector2(4, 4);
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = img;
            var ic = RT("Chevron", r); Anchor(ic, 0.5f, 0.5f, 0, 0, 5 * Q, 5 * Q); ic.pivot = new Vector2(0.5f, 0.5f);
            Img(ic, chevron, Ink, false, false);
            if (flip) ic.localScale = new Vector3(-1, 1, 1);
            return b;
        }

        static void BuildHeroPicker(Transform menu, UIManager ui)
        {
            var row = Row(menu, "HeroPicker", 40);
            var glow = RT("Glow", row); Anchor(glow, 0.5f, 0.5f, 0, 0, 48 * Q, 48 * Q); glow.pivot = new Vector2(0.5f, 0.5f);
            ui.heroGlow = Img(glow, glowSprite, C("#e8283c", 0.4f), false, false);
            var holder = RT("PreviewHolder", row); Anchor(holder, 0.5f, 0.5f, 0, 0, 40 * Q, 40 * Q); holder.pivot = new Vector2(0.5f, 0.5f);
            var prev = RT("Preview", holder); Stretch(prev);
            ui.heroPreview = Img(prev, S("hero_skythread_air"), Color.white, false, false);
            ui.heroPreview.preserveAspect = true;
            ui.prevHero = CircleButton(row, "PrevHero", true);
            Anchor((RectTransform)ui.prevHero.transform, 0, 0.5f, 0, 0, 12 * Q, 12 * Q);
            ui.nextHero = CircleButton(row, "NextHero", false);
            Anchor((RectTransform)ui.nextHero.transform, 1, 0.5f, 0, 0, 12 * Q, 12 * Q);
            ui.heroName = Txt(menu, "HeroName", "Skythread", display, 6.4f, Ink);
            ui.heroTag = Txt(menu, "HeroTag", "The original web-zipper", mono, 3.2f, Muted);
            var dots = Row(menu, "Dots", 1.4f);
            var h = dots.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 1.4f * Q; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            ui.dots = new Image[6];
            for (int i = 0; i < 6; i++)
            {
                var d = RT("Dot" + i, dots);
                ui.dots[i] = Img(d, round, Color.white, true, false, 3f);
                var le = d.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = 1.4f * Q; le.preferredHeight = 1.4f * Q;
            }
        }

        static void HowStep(Transform p, string num, string desc, bool icon)
        {
            var r = Row(p, "Step", 12); var h = HL(r, 3, false);
            if (icon)
            {
                var holder = RT("Icon", r); var le = holder.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = 9 * Q; le.minWidth = 9 * Q;
                var im = RT("Img", holder); Anchor(im, 0, 0.5f, 0, 0, 6 * Q, 6 * Q); Img(im, citizenIcon, Color.white, false, false);
            }
            else
            {
                var n = Txt(r, "Num", num, display, 6, Red, TextAnchor.MiddleLeft);
                var le = n.GetComponent<LayoutElement>(); le.preferredWidth = 9 * Q; le.minWidth = 9 * Q;
            }
            var d = Txt(r, "Desc", desc, mono, 3.8f, Soft, TextAnchor.MiddleLeft);
            d.GetComponent<LayoutElement>().flexibleWidth = 1;
        }

        static void Hazard(Transform p, string name, string floor)
        {
            var r = Row(p, "Hazard", 8); HL(r, 2, false);
            Txt(r, "Name", name, mono, 3.6f, Ink, TextAnchor.MiddleLeft).GetComponent<LayoutElement>().flexibleWidth = 1;
            Txt(r, "Floor", floor, mono, 3.6f, Muted, TextAnchor.MiddleRight).GetComponent<LayoutElement>().preferredWidth = 18 * Q;
            Divider(p);
        }

        static Button RowButton(Transform p, string name, string label, out Text value, out Text labelText)
        {
            var r = Row(p, name, 11);
            var img = Img(r, null, new Color(0, 0, 0, 0));
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = img;
            HL(r, 2, false);
            labelText = Txt(r, "Label", label, mono, 3.8f, Ink, TextAnchor.MiddleLeft);
            labelText.GetComponent<LayoutElement>().flexibleWidth = 1;
            value = Txt(r, "Value", "", monoBold, 3.8f, Red, TextAnchor.MiddleRight);
            value.GetComponent<LayoutElement>().preferredWidth = 36 * Q;
            Divider(p);
            return b;
        }
    }
}
