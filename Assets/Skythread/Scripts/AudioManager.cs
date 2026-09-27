using UnityEngine;

namespace Skythread
{
    /// <summary>
    /// All game audio in one place: one background music track (loops for the whole game)
    /// plus one-shot SFX for every gameplay and UI event. Built by Tools > Skythread > Build Scene
    /// as an "Audio" GameObject next to Game, and wired to Game.audio automatically.
    ///
    /// Every clip field starts empty. Drop AudioClips onto this component in the Inspector
    /// whenever you have them — every Play... method below already no-ops safely if its
    /// clip is unassigned, so the game runs fine with some, none, or all slots filled.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Music (one track, loops for the whole game)")]
        public AudioClip music;
        [Range(0f, 1f)] public float musicVolume = 0.5f;

        [Header("Gameplay SFX")]
        public AudioClip jump;       // web-zip jump
        public AudioClip pickup;     // citizen collected
        public AudioClip death;      // any obstacle hit
        public AudioClip newBest;    // plays alongside death when it's a new best

        [Header("Countdown SFX")]
        public AudioClip countdownTick;  // each of 3, 2, 1
        public AudioClip countdownGo;    // countdown hits zero, play starts

        [Header("UI SFX")]
        public AudioClip pauseOpen;  // entering the pause screen
        public AudioClip uiClick;    // every button in UIManager

        [Range(0f, 1f)] public float sfxVolume = 0.7f;

        AudioSource musicSource, sfxSource;

        void Awake()
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        void Start() { PlayMusic(); }

        public void PlayMusic()
        {
            if (!music) return;
            musicSource.clip = music;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        void Shot(AudioClip c) { if (c) sfxSource.PlayOneShot(c, sfxVolume); }

        public void PlayJump() { Shot(jump); }
        public void PlayPickup() { Shot(pickup); }
        public void PlayDeath() { Shot(death); }
        public void PlayNewBest() { Shot(newBest); }
        public void PlayCountdownTick() { Shot(countdownTick); }
        public void PlayCountdownGo() { Shot(countdownGo); }
        public void PlayPauseOpen() { Shot(pauseOpen); }
        public void PlayUiClick() { Shot(uiClick); }
    }
}
