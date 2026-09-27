using UnityEngine;
using UnityEngine.EventSystems;

namespace Skythread
{
    // Full-screen invisible UI image behind all menus. Any tap that isn't on a button = jump.
    public class TapCatcher : MonoBehaviour, IPointerDownHandler
    {
        public Game game;
        public void OnPointerDown(PointerEventData e) { if (game) game.FieldTap(); }
    }
}
