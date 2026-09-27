using UnityEngine;
using UnityEngine.UI;

namespace Skythread
{
    // All references are wired automatically by Tools > Skythread > Build Scene.
    public class UIManager : MonoBehaviour
    {
        public Game game;
        [Header("Panels")]
        public GameObject hud, hint, menu, over, overNewBest, overButtons, pause, countdown, how, settings;
        [Header("Hero picker (main menu)")]
        public Image heroPreview, heroGlow;
        public Image[] dots;
        public Text heroName, heroTag;
        public Button prevHero, nextHero;
        [Header("Texts")]
        public Text score, coins, menuBest, overReason, overScore, overBest, overSaved, pauseInfo, countdownText, settingsBest, rainValue, resetValue;
        [Header("Buttons")]
        public Button pauseBtn, playBtn, howBtn, settingsBtn, retryBtn, overMenuBtn, resumeBtn, restartBtn, pauseSettingsBtn, quitBtn, howBackBtn, chillBtn, normalBtn, hardBtn, rainBtn, resetBtn, settingsDoneBtn, exitBtn;

        string screen; bool confirmReset; float swapT = -10f;
        const float Q = 10.8f;
        public bool HasScreen { get { return screen != null; } }

        static readonly Color Red = new Color32(0xe8, 0x28, 0x3c, 255);
        static readonly Color Muted = new Color32(0xb4, 0xc2, 0xe0, 255);
        static readonly Color Soft = new Color32(0xdd, 0xe5, 0xf5, 255);

        public void Bind(Game g)
        {
            game = g;
            Wire(pauseBtn, game.Pause);
            Wire(prevHero, () => game.CycleChar(-1));
            Wire(nextHero, () => game.CycleChar(1));
            Wire(playBtn, game.StartRun);
            Wire(retryBtn, game.StartRun);
            Wire(restartBtn, game.StartRun);
            Wire(overMenuBtn, game.Quit);
            Wire(quitBtn, game.Quit);
            Wire(exitBtn, game.ExitApp);
            Wire(resumeBtn, game.Resume);
            Wire(howBtn, () => Open("how"));
            Wire(settingsBtn, () => Open("settings"));
            Wire(pauseSettingsBtn, () => Open("settings"));
            Wire(howBackBtn, CloseScreen);
            Wire(settingsDoneBtn, CloseScreen);
            Wire(chillBtn, () => game.SetDifficulty("chill"));
            Wire(normalBtn, () => game.SetDifficulty("normal"));
            Wire(hardBtn, () => game.SetDifficulty("hard"));
            Wire(rainBtn, game.ToggleRain);
            Wire(resetBtn, () =>
            {
                if (!confirmReset) { confirmReset = true; Refresh(); }
                else { confirmReset = false; game.ResetBest(); }
            });
        }

        // Every button routes through here so a UI click sound plays before the action runs.
        void Wire(Button b, UnityEngine.Events.UnityAction action)
        {
            b.onClick.AddListener(() => { if (game.audio) game.audio.PlayUiClick(); action(); });
        }

        public void OnCharChanged() { swapT = Time.unscaledTime; Refresh(); }

        void Update()
        {
            if (!game || !heroPreview || !menu.activeInHierarchy) return;
            float t = Time.unscaledTime, k = Mathf.Clamp01((t - swapT) / 0.26f), pop = 0.8f + 0.2f * (1 - Mathf.Pow(1 - k, 3));
            var rt = heroPreview.rectTransform;
            rt.anchoredPosition = new Vector2(0, Mathf.Sin(t * 3) * 3 * Q * 0.4f);
            rt.localScale = Vector3.one * pop;
            rt.localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(t * 2) * 0.08f * Mathf.Rad2Deg);
        }

        public void Open(string s) { screen = s; confirmReset = false; Refresh(); }
        public void CloseScreen() { screen = null; confirmReset = false; Refresh(); }
        public void CloseScreenSilently() { screen = null; confirmReset = false; }

        public void RefreshHud()
        {
            if (!game) return;
            score.text = game.score + "<size=43> fl</size>";
            coins.text = game.coins.ToString();
            hint.SetActive(game.phase == Phase.Play && game.score < 4);
        }

        public void Refresh()
        {
            if (!game) return;
            var p = game.phase;
            hud.SetActive(p == Phase.Play || p == Phase.Countdown);
            menu.SetActive(p == Phase.Menu);
            over.SetActive(p == Phase.Over);
            overNewBest.SetActive(game.isNew);
            overButtons.SetActive(game.canRetry);
            pause.SetActive(p == Phase.Paused);
            countdown.SetActive(p == Phase.Countdown);
            how.SetActive(screen == "how");
            settings.SetActive(screen == "settings");
            RefreshHud();

            menuBest.text = "BEST " + game.best + " FLOORS";
            var skin = game.Skin;
            heroPreview.sprite = skin.air;
            heroGlow.color = new Color(skin.main.r, skin.main.g, skin.main.b, 0.4f);
            heroName.text = skin.name;
            heroTag.text = skin.tagline;
            for (int i = 0; i < dots.Length; i++)
            {
                bool on = i == game.charIndex;
                dots[i].color = on ? Red : new Color(221 / 255f, 229 / 255f, 245 / 255f, 0.35f);
                dots[i].GetComponent<LayoutElement>().preferredWidth = (on ? 5f : 1.4f) * Q;
            }
            overReason.text = game.reason;
            overScore.text = game.score + "<size=54> fl</size>";
            overBest.text = "BEST " + game.best;
            overSaved.text = "SAVED " + game.coins;
            pauseInfo.text = "FLOOR " + game.score + "  \u00b7  SAVED " + game.coins;
            countdownText.text = game.countdownNum.ToString();
            settingsBest.text = "Best \u00b7 floor " + game.best;
            rainValue.text = game.rain ? "ON" : "OFF";
            rainValue.color = game.rain ? Red : Muted;
            resetValue.text = confirmReset ? "TAP TO CONFIRM" : "RESET";
            Seg(chillBtn, game.difficulty == "chill");
            Seg(normalBtn, game.difficulty == "normal");
            Seg(hardBtn, game.difficulty == "hard");
        }

        void Seg(Button b, bool on)
        {
            b.GetComponent<Image>().color = on ? Red : new Color(0, 0, 0, 0);
            b.GetComponentInChildren<Text>().color = on ? Color.white : Soft;
        }
    }
}