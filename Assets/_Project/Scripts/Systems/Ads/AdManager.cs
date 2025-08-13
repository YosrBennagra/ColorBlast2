using UnityEngine;
using System;

/// <summary>
/// Simple placeholder ad manager. Replace with real ad network integration.
/// Provides two ad types: short (non-reward) and long (reward revive).
/// Global namespace to avoid asmdef namespace coupling.
/// </summary>
public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    public event Action OnShortAdFinished;
    public event Action OnLongAdFinished;
    public event Action OnLongAdFailed;

        [Header("Simulation Settings")] 
        [SerializeField] private float shortAdDuration = 1.5f;
        [SerializeField] private float longAdDuration = 4f;
        [SerializeField] private bool logSimulation = true;

    private bool showing = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsShowingAd => showing;

    public void ShowShortAd()
    {
        if (showing) return;
        StartCoroutine(RunAd(shortAdDuration, false));
    }

    public void ShowLongAd()
    {
        if (showing) return;
        StartCoroutine(RunAd(longAdDuration, true));
    }

    private System.Collections.IEnumerator RunAd(float duration, bool reward)
    {
        showing = true;
        if (logSimulation) Debug.Log($"[AdManager] Showing {(reward ? "LONG(reward)" : "SHORT")} ad for {duration:F1}s");
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        showing = false;
        if (reward) OnLongAdFinished?.Invoke(); else OnShortAdFinished?.Invoke();
    }
}
