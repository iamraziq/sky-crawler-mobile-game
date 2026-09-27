using System.Collections.Generic;
using UnityEngine;

namespace Skythread
{
    public enum Phase { Menu, Play, Paused, Countdown, Over }
    public enum ObType { Rig, Sign, Laser, Drone, Mine, Civ }

    public class Ob
    {
        public ObType t; public int side; public float x, y, vx, vy, r, len, seed, rot; public int variant; public bool dead;
        public Transform root; public SpriteRenderer a, b, c;
    }
    class Spark { public float x, y, vx, vy, life, max, r, grav; public Color col; public SpriteRenderer sr; }
    class WebSplat { public float x, y, s; public int side; public SpriteRenderer sr; }
    class Drop { public float x, y, l, v; public SpriteRenderer sr; }

    /// <summary>
    /// Direct port of the HTML prototype. All gameplay runs in "logical pixels"
    /// (390 wide, y pointing DOWN, like the canvas version). P() converts to world units.
    /// 1 logical px = 0.01 world units. Sprites are exported at 4x with 400 PPU to match.
    /// </summary>
    public class Game : MonoBehaviour
    {
        public SkythreadArt art;
        public UIManager ui;
        public Camera cam;
        public AudioManager audio;

        public const float W = 390f, WW = 48f, JumpDur = 0.34f, PX = 0.01f;
        public static readonly string[] Words = { "PIZZA", "HOTEL", "COLA", "OPEN" };
        [HideInInspector] public float H = 780f;

        [Header("Runtime state (read by UI)")]
        public Phase phase = Phase.Menu;
        public int best, score, coins, countdownNum = 3, charIndex;
        public string difficulty = "normal";
        public bool rain = true, canRetry, isNew;
        public string reason = "";

        float scroll, dist, speed, spawnAcc, nextGap = 560, time, jt, cd, deadT, shake, lastUi;
        int side, from, to = 1;
        bool jumping, buffer;
        float deadX, deadY, dvx, dvy, drot;

        readonly List<Ob> obs = new List<Ob>();
        readonly List<Spark> parts = new List<Spark>();
        readonly Stack<SpriteRenderer> dotPool = new Stack<SpriteRenderer>();
        readonly List<WebSplat> webs = new List<WebSplat>();
        readonly List<Drop> drops = new List<Drop>();
        readonly List<SpriteRenderer> wallL = new List<SpriteRenderer>(), wallR = new List<SpriteRenderer>(), far = new List<SpriteRenderer>();
        SpriteRenderer hero, targetSplat, fadeTop, glowBottom, rimL, rimR, webMain, webAccent;
        Transform world;

        // ---------- difficulty (base speed, accel, max speed, gap multiplier) ----------
        struct Cfg { public float b, a, m, g; public Cfg(float b, float a, float m, float g) { this.b = b; this.a = a; this.m = m; this.g = g; } }
        Cfg C()
        {
            switch (difficulty)
            {
                case "chill": return new Cfg(160, 4, 460, 1.25f);
                case "hard": return new Cfg(250, 9, 690, 0.85f);
                default: return new Cfg(190, 6, 570, 1f);
            }
        }

        static float Rnd() { return UnityEngine.Random.value; }
        static float Sq(float v) { return v * v; }
        Vector3 P(float x, float y) { return new Vector3((x - W * 0.5f) * PX, (H * 0.5f - y) * PX, 0f); }
        static Color Hex(string h, float a = 1f) { Color c; ColorUtility.TryParseHtmlString(h, out c); c.a = a; return c; }

        // ---------- lifecycle ----------
        void Awake()
        {
            best = PlayerPrefs.GetInt("skythread_best", 0);
            difficulty = PlayerPrefs.GetString("skythread_diff", "normal");
            rain = PlayerPrefs.GetInt("skythread_rain", 1) == 1;
            charIndex = PlayerPrefs.GetInt("skythread_char", 0);
            Application.targetFrameRate = 60;
        }

        void Start()
        {
            if (!cam) cam = Camera.main;
            FitCamera();
            world = new GameObject("World").transform;
            world.SetParent(transform, false);
            BuildScenery();
            ResetRun();
            if (ui) ui.Bind(this);
            RefreshUI();
        }

        void FitCamera()
        {
            cam.orthographic = true;
            float aspect = Mathf.Max(0.3f, cam.aspect);
            H = W / aspect;                          // width is fixed, height follows the phone
            cam.orthographicSize = H * 0.5f * PX;
        }

        void Update()
        {
            FitCamera();
            HandleKeys();
            Step(Mathf.Min(0.033f, Time.unscaledDeltaTime));
            Render();
        }

        void OnApplicationPause(bool p) { if (p) Pause(); }
        void OnApplicationFocus(bool f) { if (!f) Pause(); }

        void HandleKeys()
        {
            bool jumpKey, escKey, leftKey, rightKey;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            jumpKey = kb != null && (kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
            escKey = kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame);
            leftKey = kb != null && kb.leftArrowKey.wasPressedThisFrame;
            rightKey = kb != null && kb.rightArrowKey.wasPressedThisFrame;
#else
            jumpKey = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Return);
            escKey = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
            leftKey = Input.GetKeyDown(KeyCode.LeftArrow);
            rightKey = Input.GetKeyDown(KeyCode.RightArrow);
#endif
            bool screenOpen = ui && ui.HasScreen;
            if (phase == Phase.Menu && !screenOpen && (leftKey || rightKey)) { CycleChar(leftKey ? -1 : 1); return; }
            if (escKey)
            {
                if (screenOpen) ui.CloseScreen();
                else if (phase == Phase.Play || phase == Phase.Countdown) Pause();
                else if (phase == Phase.Paused) Resume();
            }
            else if (jumpKey && !screenOpen) Tap();
        }

        // ---------- public actions (UI buttons call these) ----------
        public void StartRun() { ResetRun(); phase = Phase.Play; reason = ""; canRetry = false; isNew = false; if (ui) ui.CloseScreenSilently(); RefreshUI(); }
        public void Pause() { if (phase == Phase.Play || phase == Phase.Countdown) { phase = Phase.Paused; if (audio) audio.PlayPauseOpen(); RefreshUI(); } }
        public void Resume() { if (phase != Phase.Paused) return; cd = 3; countdownNum = 3; phase = Phase.Countdown; if (audio) audio.PlayCountdownTick(); RefreshUI(); }
        public void Quit() { ResetRun(); phase = Phase.Menu; if (ui) ui.CloseScreenSilently(); RefreshUI(); }
        public void ExitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        public void Tap()
        {
            if (phase == Phase.Play) Jump();
            else if (phase == Phase.Menu) StartRun();
            else if (phase == Phase.Over && canRetry) StartRun();
            else if (phase == Phase.Paused) Resume();
        }
        public void FieldTap() { if (phase == Phase.Play) Jump(); }
        public void SetDifficulty(string d) { difficulty = d; PlayerPrefs.SetString("skythread_diff", d); PlayerPrefs.Save(); RefreshUI(); }
        public void ToggleRain() { rain = !rain; PlayerPrefs.SetInt("skythread_rain", rain ? 1 : 0); PlayerPrefs.Save(); RefreshUI(); }
        public void ResetBest() { best = 0; PlayerPrefs.SetInt("skythread_best", 0); PlayerPrefs.Save(); RefreshUI(); }
        void RefreshUI() { if (ui) ui.Refresh(); }

        public HeroSkin Skin { get { return art.heroes[Mathf.Clamp(charIndex, 0, art.heroes.Length - 1)]; } }
        public void CycleChar(int dir)
        {
            int n = art.heroes.Length;
            charIndex = ((charIndex + dir) % n + n) % n;
            PlayerPrefs.SetInt("skythread_char", charIndex); PlayerPrefs.Save();
            if (ui) ui.OnCharChanged();
        }

        void Jump()
        {
            if (jumping) { if (jt > 0.55f) buffer = true; return; }
            jumping = true; jt = 0; from = side; to = 1 - side;
            if (audio) audio.PlayJump();
        }

        void ResetRun()
        {
            foreach (var o in obs) KillView(o); obs.Clear();
            foreach (var w in webs) Destroy(w.sr.gameObject); webs.Clear();
            foreach (var p in parts) Recycle(p.sr); parts.Clear();
            dist = 0; speed = C().b; spawnAcc = 0; nextGap = 560; side = 0; jumping = false; jt = 0; from = 0; to = 1;
            buffer = false; coins = 0; score = 0; lastUi = 0; shake = 0; deadT = 0;
        }

        // x, y, rotation(rad, canvas-style clockwise)
        Vector3 Pos()
        {
            float L = WW + 12, R = W - WW - 12, baseY = H * 0.7f;
            if (!jumping) return new Vector3(side == 1 ? R : L, baseY + (phase == Phase.Menu ? Mathf.Sin(time * 3) * 1.5f : 0), 0);
            float t = jt, e = t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
            float a = from == 1 ? R : L, b = to == 1 ? R : L;
            return new Vector3(a + (b - a) * e, baseY - Mathf.Sin(Mathf.PI * t) * 44, (to == 1 ? 1 : -1) * Mathf.PI * 2 * e);
        }

        // ---------- collisions ----------
        Rect? Box(Ob o)
        {
            switch (o.t)
            {
                case ObType.Rig: return new Rect(o.side == 1 ? W - WW - 56 : WW, o.y - 14, 56, 30);
                case ObType.Sign: return new Rect(o.side == 1 ? W - WW - 46 : WW + 12, o.y - 58, 34, 116);
                case ObType.Laser: return new Rect(o.side == 1 ? W - WW - o.len : WW, o.y - 6, o.len, 12);
            }
            return null;
        }

        bool Hit(Ob o, Vector3 p)
        {
            float cx = p.x, cy = p.y - 3, r = 11;
            if (o.t == ObType.Civ) return Sq(o.x - cx) + Sq(o.y - 8 - cy) < Sq(r + 20);
            if (o.t == ObType.Drone || o.t == ObType.Mine) return Sq(o.x - cx) + Sq(o.y - cy) < Sq(r + o.r);
            Rect b = Box(o).Value;
            float qx = Mathf.Clamp(cx, b.xMin, b.xMax), qy = Mathf.Clamp(cy, b.yMin, b.yMax);
            return Sq(qx - cx) + Sq(qy - cy) < r * r;
        }

        // ---------- spawning (gentle start, types unlock by distance) ----------
        void Spawn()
        {
            float d = dist; int s = Rnd() < 0.5f ? 0 : 1; float y = -90;
            var names = new List<string> { "wall" }; var wts = new List<float> { 3 };
            if (d > 1600) { names.Add("laser"); wts.Add(Mathf.Min(2.2f, 0.8f + (d - 1600) / 2000f)); }
            if (d > 4000) { names.Add("drone"); wts.Add(Mathf.Min(1.6f, 0.6f + (d - 4000) / 3000f)); }
            if (d > 7000) { names.Add("mine"); wts.Add(Mathf.Min(1.3f, 0.5f + (d - 7000) / 3000f)); }
            float sum = 0; foreach (var w in wts) sum += w; sum *= Rnd();
            string kind = names[0];
            for (int i = 0; i < names.Count; i++) { sum -= wts[i]; if (sum <= 0) { kind = names[i]; break; } }

            var o = new Ob { side = s, y = y, seed = Rnd() * 10 };
            switch (kind)
            {
                case "wall": o.t = Rnd() < 0.55f ? ObType.Rig : ObType.Sign; o.variant = Mathf.Min(3, (int)(Rnd() * 4)); break;
                case "laser": o.t = ObType.Laser; o.len = (W - 2 * WW) * (0.4f + Rnd() * 0.12f + 0.12f * Mathf.Min(1, (d - 1600) / 6000f)); break;
                case "drone":
                    o.t = ObType.Drone; o.x = WW + 30 + Rnd() * (W - 2 * WW - 60);
                    o.vx = (Rnd() < 0.5f ? -1 : 1) * (60 + Rnd() * 50 + Mathf.Min(110, (d - 4000) / 60f)); o.r = 13; break;
                default:
                    o.t = ObType.Mine; o.x = s == 1 ? W - WW - 16 : WW + 16; o.y = -320; o.vy = 240; o.r = 12; o.rot = Rnd() * 6; break;
            }
            MakeView(o); obs.Add(o);

            if (Rnd() < 0.4f)
            {
                var c = new Ob { t = ObType.Civ, x = W / 2 + (Rnd() - 0.5f) * 40, y = y + nextGap * 0.5f, seed = Rnd() * 10, variant = Mathf.Min(3, (int)(Rnd() * 4)) };
                MakeView(c); obs.Add(c);
            }
        }

        void Die(Ob o)
        {
            var p = Pos();
            deadX = p.x; deadY = p.y; dvx = (p.x < W / 2 ? 1 : -1) * 140; dvy = -420; drot = p.z; shake = 14; deadT = 0;
            Puff(p.x, p.y, 22, Color.white, 260); Puff(p.x, p.y, 10, Skin.main, 200);
            string[] lines;
            switch (o.t)
            {
                case ObType.Rig: lines = new[] { "Squeegeed.", "Should have tipped the window washer." }; break;
                case ObType.Sign: lines = new[] { "Neon sign: 1. Hero: 0.", "Distracted by the " + Words[o.variant].ToLower() + " sign." }; break;
                case ObType.Laser: lines = new[] { "Tripped the laser grid.", "Zapped. Dr. Volt is smug now." }; break;
                case ObType.Drone: lines = new[] { "A Volt drone got the drop on you.", "Tagged by a drone." }; break;
                default: lines = new[] { "Heads up \u2014 boom.", "Should have looked up." }; break;
            }
            score = Mathf.FloorToInt(dist / 20); int prev = best; best = Mathf.Max(score, prev);
            PlayerPrefs.SetInt("skythread_best", best); PlayerPrefs.Save();
            isNew = score > prev && score > 0;
            reason = lines[Mathf.Min(lines.Length - 1, (int)(Rnd() * lines.Length))];
            if (audio) { audio.PlayDeath(); if (isNew) audio.PlayNewBest(); }
            canRetry = false; phase = Phase.Over; RefreshUI();
        }

        // ---------- simulation ----------
        void Step(float dt)
        {
            if (phase == Phase.Paused) return;
            if (phase == Phase.Countdown)
            {
                cd -= dt; int n = Mathf.Max(1, Mathf.CeilToInt(cd));
                if (cd <= 0) { phase = Phase.Play; if (audio) audio.PlayCountdownGo(); RefreshUI(); }
                else if (n != countdownNum) { countdownNum = n; if (audio) audio.PlayCountdownTick(); RefreshUI(); }
                return;
            }
            time += dt; float dy = 0;
            if (rain) foreach (var d in drops) { d.y += d.v * dt; d.x -= d.v * 0.15f * dt; if (d.y > H) { d.y = -20; d.x = Rnd() * (W + 100); } }

            if (phase == Phase.Play)
            {
                var cf = C();
                speed = Mathf.Min(cf.m, speed + cf.a * dt); dy = speed * dt; scroll += dy; dist += dy;
                if (jumping)
                {
                    jt += dt / JumpDur;
                    if (jt >= 1)
                    {
                        jumping = false; jt = 0; side = to;
                        var w = new WebSplat { x = side == 1 ? W - WW : WW, y = H * 0.7f - 3, side = side, s = 0.8f + Rnd() * 0.4f };
                        w.sr = SR("Splat", art.webSplat, 2); w.sr.flipX = side == 1; w.sr.transform.localScale = Vector3.one * w.s; webs.Add(w);
                        if (webs.Count > 14) { Destroy(webs[0].sr.gameObject); webs.RemoveAt(0); }
                        Puff(side == 1 ? W - WW : WW, H * 0.7f, 5, Hex("#e8e6f5"), 80);
                        if (buffer) { buffer = false; Jump(); }
                    }
                }
                spawnAcc += dy;
                if (spawnAcc >= nextGap)
                {
                    spawnAcc = 0; Spawn();
                    float prog = Mathf.Min(1, dist / 16000f), ease = 1 + 1.3f * Mathf.Max(0, 1 - dist / 5000f);
                    nextGap = Mathf.Max(150, speed * (0.62f + Rnd() * 0.38f) * cf.g * ease * (1 - 0.22f * prog));
                }
                var p = Pos(); float mn = WW + 16, mx = W - WW - 16;
                foreach (var o in obs)
                {
                    o.y += dy + o.vy * dt;
                    if (o.t == ObType.Drone)
                    {
                        o.x += o.vx * dt;
                        if (o.x < mn) { o.x = mn; o.vx = Mathf.Abs(o.vx); }
                        if (o.x > mx) { o.x = mx; o.vx = -Mathf.Abs(o.vx); }
                    }
                    if (o.t == ObType.Mine) o.rot += dt * 3;
                    if (!o.dead && Hit(o, p))
                    {
                        if (o.t == ObType.Civ) { o.dead = true; coins++; if (audio) audio.PlayPickup(); Puff(o.x, o.y - 10, 14, Hex("#ffd84a"), 160, 120); }
                        else { Die(o); break; }
                    }
                }
                for (int i = obs.Count - 1; i >= 0; i--) if (obs[i].dead || obs[i].y > H + 160) { KillView(obs[i]); obs.RemoveAt(i); }
                foreach (var w in webs) w.y += dy;
                for (int i = webs.Count - 1; i >= 0; i--) if (webs[i].y > H + 30) { Destroy(webs[i].sr.gameObject); webs.RemoveAt(i); }
                if (phase == Phase.Play && time - lastUi > 0.1f) { lastUi = time; score = Mathf.FloorToInt(dist / 20); if (ui) ui.RefreshHud(); }
            }
            else if (phase == Phase.Over)
            {
                deadT += dt; dvy += 1500 * dt; deadX += dvx * dt; deadY += dvy * dt; drot += dt * 9;
                if (deadT > 0.7f && !canRetry) { canRetry = true; RefreshUI(); }
            }
            else { dy = 40 * dt; scroll += dy; }

            for (int i = parts.Count - 1; i >= 0; i--)
            {
                var q = parts[i]; q.x += q.vx * dt; q.y += q.vy * dt; q.vy += q.grav * dt; q.life -= dt;
                if (q.life <= 0) { Recycle(q.sr); parts.RemoveAt(i); }
            }
            shake = Mathf.Max(0, shake - dt * 40);
        }

        // ---------- views ----------
        SpriteRenderer SR(string name, Sprite s, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent ? parent : world, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sortingOrder = order;
            return sr;
        }

        void BuildScenery()
        {
            for (int i = 0; i < 4; i++) far.Add(SR("CityFar", art.cityFar, -10));
            for (int i = 0; i < 16; i++)
            {
                wallL.Add(SR("WallL", art.wallTile, 0));
                var r = SR("WallR", art.wallTile, 0); r.flipX = true; wallR.Add(r);
            }
            rimL = SR("RimL", art.pixel, 1); rimR = SR("RimR", art.pixel, 1);
            rimL.color = rimR.color = Hex("#3d7bff", 0.35f);
            hero = SR("Hero", Skin.wall, 10);
            targetSplat = SR("WebTarget", art.webSplat, 2);
            webMain = SR("WebLine", art.pixel, 9); webAccent = SR("WebAccent", art.pixel, 9);
            for (int i = 0; i < 80; i++)
            {
                var d = new Drop { x = Rnd() * (W + 100), y = Rnd() * H, l = 8 + Rnd() * 14, v = 700 + Rnd() * 400 };
                d.sr = SR("Rain", art.rain, 12); d.sr.color = new Color(180 / 255f, 190 / 255f, 1f, 0.2f); drops.Add(d);
            }
            fadeTop = SR("FadeTop", art.fadeTop, 20); glowBottom = SR("GlowBottom", art.glowBottom, 20);
        }

        void MakeView(Ob o)
        {
            var root = new GameObject(o.t.ToString()).transform; root.SetParent(world, false); o.root = root;
            float dir = o.side == 1 ? -1 : 1;
            switch (o.t)
            {
                case ObType.Rig:
                    o.a = SR("Rig", art.rig, 4, root); o.a.flipX = o.side == 1; break;
                case ObType.Sign:
                    o.b = SR("Bracket", art.signBracket, 4, root); o.b.flipX = o.side == 1;
                    o.a = SR("Sign", art.signs[o.variant], 5, root); o.a.transform.localPosition = new Vector3(dir * 29 * PX, 0, 0); break;
                case ObType.Laser:
                    o.a = SR("Emitter", art.laserEmitter, 5, root); o.a.flipX = o.side == 1;
                    o.b = SR("Beam", art.laserBeam, 4, root);
                    o.b.transform.localPosition = new Vector3(dir * (10 + (o.len - 10) * 0.5f) * PX, 0, 0);
                    o.b.transform.localScale = new Vector3((o.len - 10) / 8f, 1, 1);
                    o.c = SR("Node", art.laserNode, 5, root); o.c.transform.localPosition = new Vector3(dir * o.len * PX, 0, 0); break;
                case ObType.Drone:
                    o.a = SR("Drone", art.drone[0], 5, root); break;
                case ObType.Mine:
                    o.a = SR("Mine", art.mineOff, 5, root); o.b = SR("Warn", art.warn, 15); o.b.enabled = false; break;
                case ObType.Civ:
                    o.a = SR("Glow", art.civGlow, 3, root); o.a.transform.localPosition = new Vector3(0, 10 * PX, 0);
                    o.b = SR("Ring", art.civRing, 3, root); o.b.transform.localPosition = new Vector3(0, 10 * PX, 0);
                    o.c = SR("Citizen", art.civ[o.variant], 4, root); o.c.transform.localPosition = new Vector3(0, 28 * PX, 0); break;
            }
        }

        void KillView(Ob o)
        {
            if (o.root) Destroy(o.root.gameObject);
            if (o.t == ObType.Mine && o.b) Destroy(o.b.gameObject);
        }

        void Puff(float x, float y, int n, Color col, float spd, float grav = 300)
        {
            for (int i = 0; i < n; i++)
            {
                float life = 0.35f + Rnd() * 0.35f;
                var p = new Spark { x = x, y = y, vx = (Rnd() - 0.5f) * spd * 2, vy = (Rnd() - 0.5f) * spd * 2 - 40, life = life, max = life, r = 1.5f + Rnd() * 2.5f, col = col, grav = grav };
                p.sr = Dot(); parts.Add(p);
            }
        }
        SpriteRenderer Dot()
        {
            if (dotPool.Count > 0) { var s = dotPool.Pop(); s.gameObject.SetActive(true); return s; }
            return SR("Dot", art.dot, 8);
        }
        void Recycle(SpriteRenderer s) { s.gameObject.SetActive(false); dotPool.Push(s); }

        void SetLine(SpriteRenderer sr, float x1, float y1, float x2, float y2, float w, Color c)
        {
            Vector3 a = P(x1, y1), b = P(x2, y2), d = b - a;
            sr.transform.position = (a + b) * 0.5f;
            sr.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            sr.transform.localScale = new Vector3(d.magnitude / PX, w, 1);
            sr.color = c;
        }

        void Render()
        {
            var camPos = new Vector3(0, 0, -10);
            if (shake > 0) camPos += new Vector3((Rnd() - 0.5f) * shake * PX, (Rnd() - 0.5f) * shake * PX, 0);
            cam.transform.position = camPos;

            float fh = 960, fo = (scroll * 0.3f) % fh;
            for (int k = 0; k < far.Count; k++) far[k].transform.position = P(W / 2, fo - fh + k * fh + fh / 2);
            float th = 192, off = scroll % th;
            for (int i = 0; i < wallL.Count; i++)
            {
                float top = off - th + i * th;
                wallL[i].transform.position = P(WW / 2, top + th / 2);
                wallR[i].transform.position = P(W - WW / 2, top + th / 2);
            }
            rimL.transform.position = P(WW + 0.5f, H / 2); rimL.transform.localScale = new Vector3(1, H, 1);
            rimR.transform.position = P(W - WW - 0.5f, H / 2); rimR.transform.localScale = new Vector3(1, H, 1);
            foreach (var w in webs) w.sr.transform.position = P(w.x, w.y);

            foreach (var o in obs)
            {
                float wx = o.side == 1 ? W - WW : WW;
                switch (o.t)
                {
                    case ObType.Rig: o.root.position = P(wx, o.y); break;
                    case ObType.Sign:
                        {
                            o.root.position = P(wx, o.y);
                            float fl = Mathf.Sin(time * 31 + o.seed * 7) > 0.94f ? 0.55f : 1f;
                            o.a.color = new Color(fl, fl, fl, 1); break;
                        }
                    case ObType.Laser:
                        {
                            o.root.position = P(wx, o.y);
                            o.b.color = new Color(1, 1, 1, 0.7f + 0.3f * Mathf.Sin(time * 18 + o.seed)); break;
                        }
                    case ObType.Drone:
                        o.root.position = P(o.x, o.y + Mathf.Sin(time * 5 + o.x * 0.05f) * 3);
                        o.root.rotation = Quaternion.Euler(0, 0, -(o.vx > 0 ? 1 : -1) * 0.12f * Mathf.Rad2Deg);
                        o.a.sprite = art.drone[(int)(time * 24) % art.drone.Length]; break;
                    case ObType.Mine:
                        o.root.position = P(o.x, o.y);
                        o.root.rotation = Quaternion.Euler(0, 0, -o.rot * Mathf.Rad2Deg);
                        o.a.sprite = Mathf.FloorToInt(time * 6) % 2 == 1 ? art.mineOn : art.mineOff;
                        o.b.enabled = o.y < -15 && Mathf.FloorToInt(time * 8) % 2 == 0;
                        o.b.transform.position = P(o.x, 128.5f); break;
                    case ObType.Civ:
                        {
                            float sw = Mathf.Sin(time * 2.2f + o.seed) * 0.15f, pu = 0.5f + 0.5f * Mathf.Sin(time * 5 + o.seed), bob = Mathf.Sin(time * 2 + o.seed) * 3;
                            o.root.position = P(o.x, o.y + bob);
                            o.a.transform.localScale = Vector3.one * ((40 + pu * 6) / 46f);
                            o.b.transform.localScale = Vector3.one * ((22 + pu * 8) / 30f);
                            o.b.color = new Color(1, 1, 1, 0.6f - pu * 0.45f);
                            o.c.transform.localRotation = Quaternion.Euler(0, 0, -sw * Mathf.Rad2Deg); break;
                        }
                }
            }

            bool over = phase == Phase.Over;
            var hp = Pos();
            bool web = !over && jumping && jt < 0.85f;
            webMain.enabled = webAccent.enabled = targetSplat.enabled = web;
            if (web)
            {
                float tx = to == 1 ? W - WW : WW, ty = H * 0.7f - 70, a = 1 - jt / 0.85f;
                SetLine(webMain, hp.x, hp.y - 8, tx, ty, 1.6f, new Color(0.925f, 0.94f, 1f, 0.85f * a));
                SetLine(webAccent, hp.x + 1, hp.y - 7, tx, ty + 2, 0.8f, Hex("#3d7bff", 0.5f * a));
                targetSplat.transform.position = P(tx, ty); targetSplat.transform.localScale = Vector3.one * 0.6f; targetSplat.flipX = to == 1;
            }

            float x, y, rot; int sd; bool onWall;
            if (over) { x = deadX; y = deadY; rot = drot; sd = side; onWall = false; }
            else { x = hp.x; y = hp.y; rot = hp.z; sd = jumping ? to : side; onWall = !jumping; }
            hero.sprite = onWall ? Skin.wall : Skin.air;
            hero.transform.position = P(x, y);
            hero.transform.rotation = Quaternion.Euler(0, 0, -rot * Mathf.Rad2Deg);
            hero.transform.localScale = new Vector3(sd == 0 ? 1 : -1, 1, 1);

            foreach (var d in drops)
            {
                d.sr.enabled = rain; if (!rain) continue;
                d.sr.transform.position = P(d.x - d.l * 0.075f, d.y + d.l * 0.5f);
                d.sr.transform.rotation = Quaternion.Euler(0, 0, -8.53f);
                d.sr.transform.localScale = new Vector3(1, d.l / 14f, 1);
            }
            foreach (var q in parts)
            {
                q.sr.transform.position = P(q.x, q.y); q.sr.transform.localScale = Vector3.one * (q.r / 4f);
                var c = q.col; c.a = Mathf.Clamp01(q.life / q.max); q.sr.color = c;
            }
            fadeTop.transform.position = P(W / 2, 100);
            glowBottom.transform.position = P(W / 2, H - 110);
        }
    }
}