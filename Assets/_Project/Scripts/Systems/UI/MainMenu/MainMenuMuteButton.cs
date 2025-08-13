using UnityEngine;
using UnityEngine.UI;

namespace ColorBlast2.UI.MainMenu
{
    /// <summary>
    /// Handles mute button logic for main menu.
    /// </summary>
    /// <summary>
    /// Attach to a mute button GameObject in MainMenu scene.
    /// Assign button, icon, onSprite, and offSprite in the Inspector.
    /// </summary>
    public class MainMenuMuteButton : MonoBehaviour
    {
        public Button button;
        public Image icon;
        public Sprite onSprite;
        public Sprite offSprite;

        public void SetIcon(bool muted)
        {
            if (icon != null)
                icon.sprite = muted && offSprite != null ? offSprite : onSprite;
        }
    }
}
