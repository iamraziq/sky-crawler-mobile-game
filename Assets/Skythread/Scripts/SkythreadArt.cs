using UnityEngine;

namespace Skythread
{
    // Holds every sprite the game uses. Created automatically by Tools > Skythread > Build Scene.
    [System.Serializable]
    public class HeroSkin
    {
        public string id, name, tagline;
        public Color main;
        public Sprite wall, air;
    }

    [CreateAssetMenu(menuName = "Skythread/Art Library")]
    public class SkythreadArt : ScriptableObject
    {
        [Header("Heroes (main-menu picker, in order)")]
        public HeroSkin[] heroes;
        public Sprite webSplat;

        [Header("Obstacles")]
        public Sprite rig;
        public Sprite signBracket;
        public Sprite[] signs;          // PIZZA, HOTEL, COLA, OPEN (same order as Game.Words)
        public Sprite laserEmitter;
        public Sprite laserBeam;        // 8 logical px wide strip, stretched on X
        public Sprite laserNode;
        public Sprite[] drone;          // rotor animation frames
        public Sprite mineOn;
        public Sprite mineOff;
        public Sprite warn;

        [Header("Collectible")]
        public Sprite[] civ;            // 4 shirt colours, pivot = umbrella centre
        public Sprite civGlow;
        public Sprite civRing;

        [Header("Scenery / FX")]
        public Sprite wallTile;         // 48 x 192 logical px, left wall orientation
        public Sprite cityFar;          // 390 x 960 logical px, tiles vertically
        public Sprite fadeTop;
        public Sprite glowBottom;
        public Sprite dot;              // particle, 8 logical px diameter
        public Sprite pixel;            // 1 logical px white square
        public Sprite rain;             // 14 logical px streak
    }
}
