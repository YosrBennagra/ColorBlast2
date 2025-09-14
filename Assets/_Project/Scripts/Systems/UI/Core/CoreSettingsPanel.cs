using UnityEngine;

namespace ShapeBlaster.UI.Core
{
    /// <summary>
    /// Handles settings panel logic (show/hide, sorting order, etc).
    /// </summary>
    /// <summary>
    /// Attach to the settings panel GameObject in CoreGame scene.
    /// Handles show/hide and always-on-top logic.
    /// </summary>
    public class CoreSettingsPanel : MonoBehaviour
    {
        private Canvas panelCanvas;

        [Header("Settings Panel Buttons")]
    public UnityEngine.UI.Button muteBgmButton;
    public UnityEngine.UI.Button muteSfxButton;
    [Header("Mute Button Images")]
    public UnityEngine.UI.Image muteBgmImage;
    public UnityEngine.UI.Image muteSfxImage;
        public UnityEngine.UI.Button resumeButton;
        public UnityEngine.UI.Button restartButton;
        public UnityEngine.UI.Button homeButton;

        public System.Action OnResume;
        public System.Action OnRestart;
        public System.Action OnHome;

    private void Awake()
        {
            panelCanvas = GetComponent<Canvas>();
            if (panelCanvas == null)
                panelCanvas = gameObject.AddComponent<Canvas>();
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 999;

            // Ensure GraphicRaycaster exists on canvas
            if (panelCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            {
                panelCanvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            SetupButton(muteBgmButton, ToggleMusicMute);
            SetupButton(muteSfxButton, ToggleSfxMute);
            SetupButton(resumeButton, () => { Hide(); OnResume?.Invoke(); });
            SetupButton(restartButton, () => OnRestart?.Invoke());
            SetupButton(homeButton, () => OnHome?.Invoke());

            // Update button visuals to match current saved state
            UpdateMuteButtonVisuals();
        }

        private void Start()
        {
            // Ensure button visuals match the actual audio state on scene start
            UpdateMuteButtonVisuals();
        }

        private void SetupButton(UnityEngine.UI.Button button, System.Action action)
        {
            if (button != null) 
            {
                button.interactable = true;
                
                // Ensure raycast targets are enabled
                var buttonImage = button.GetComponent<UnityEngine.UI.Image>();
                if (buttonImage != null) buttonImage.raycastTarget = true;
                
                var buttonText = button.GetComponentInChildren<UnityEngine.UI.Text>();
                if (buttonText != null) buttonText.raycastTarget = true;
                
                var buttonTMP = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (buttonTMP != null) buttonTMP.raycastTarget = true;
                
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => action?.Invoke());
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            Time.timeScale = 0f; // Pause game
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f; // Resume game
        }

        public void Toggle()
        {
            bool next = !gameObject.activeSelf;
            gameObject.SetActive(next);
            Time.timeScale = next ? 0f : 1f;
        }

        private void ToggleMusicMute()
        {
            // Use AudioManager's toggle for music
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ToggleMusic();
            }
            UpdateMuteButtonVisuals();
        }

        private void ToggleSfxMute()
        {
            bool muted = !IsSfxMuted();
            PlayerPrefs.SetInt("Audio.SfxMuted", muted ? 1 : 0);
            PlayerPrefs.Save();
            UpdateMuteButtonVisuals();
        }

        private void UpdateMuteButtonVisuals()
        {
            float onAlpha = 1f;
            float offAlpha = 0.4f;
            bool musicMuted = false;
            if (AudioManager.Instance != null)
                musicMuted = AudioManager.Instance.IsMusicMuted();
            else
                musicMuted = PlayerPrefs.GetInt("Audio.MusicMuted", 0) == 1;
            if (muteBgmImage != null)
            {
                var c = muteBgmImage.color;
                c.a = musicMuted ? offAlpha : onAlpha;
                muteBgmImage.color = c;
            }
            if (muteSfxImage != null)
            {
                var c = muteSfxImage.color;
                c.a = IsSfxMuted() ? offAlpha : onAlpha;
                muteSfxImage.color = c;
            }
        }

        private bool IsMusicMuted() => PlayerPrefs.GetInt("Audio.MusicMuted", 0) == 1;
        private bool IsSfxMuted() => PlayerPrefs.GetInt("Audio.SfxMuted", 0) == 1;
    }
}
