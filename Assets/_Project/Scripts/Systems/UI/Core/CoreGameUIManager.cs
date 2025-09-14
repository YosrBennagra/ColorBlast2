using UnityEngine;

namespace ShapeBlaster.UI.Core
{
    /// <summary>
    /// Main UI controller for the CoreGame scene. Coordinates panels, score, settings, mute, etc.
    /// </summary>
    /// <summary>
    /// Attach this to a root UI GameObject in the CoreGame scene.
    /// Assign all references in the Inspector for modular UI control.
    /// </summary>
    public class CoreGameUIManager : MonoBehaviour
    {
        // Reference to other UI components (set in Inspector)
    public CoreScoreDisplay scoreDisplay;
    public CoreSettingsPanel settingsPanel;
    // UI Management for the core gameplay scene
    // Note: These scripts manage UI overlays for game state transitions
    // Removed: public ShapeBlaster.UI.Audio.BackgroundMusicController bgmController;
    public UnityEngine.UI.Button settingsButton;

        private void Start()
        {
            // Ensure settings panel is hidden on start
            if (settingsPanel != null)
                settingsPanel.Hide();
            // Wire up settings button to open settings panel
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettingsPanel);
        }

        // Call this from your settings button OnClick
        public void OpenSettingsPanel()
        {
            if (settingsPanel != null)
                settingsPanel.Show();
            if (settingsButton != null)
                StartCoroutine(RotateButton(settingsButton.transform, 0.2f, 180f));
        }

        private System.Collections.IEnumerator RotateButton(Transform target, float duration, float angle)
        {
            if (target == null) yield break;
            float startZ = target.eulerAngles.z;
            float endZ = startZ + angle;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float z = Mathf.LerpAngle(startZ, endZ, t);
                var e = target.eulerAngles;
                e.z = z;
                target.eulerAngles = e;
                yield return null;
            }
            var finalE = target.eulerAngles;
            finalE.z = endZ;
            target.eulerAngles = finalE;
        }

    // UI management for core gameplay
    }
}
