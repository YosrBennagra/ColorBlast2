using UnityEngine;

namespace ShapeBlaster.UI.MainMenu
{
    /// <summary>
    /// Handles settings panel logic for main menu.
    /// </summary>
    public class MainMenuSettingsPanel : MonoBehaviour
    {
        private Canvas panelCanvas;

        private void Awake()
        {
            panelCanvas = GetComponent<Canvas>();
            if (panelCanvas == null)
                panelCanvas = gameObject.AddComponent<Canvas>();
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 999;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
}
