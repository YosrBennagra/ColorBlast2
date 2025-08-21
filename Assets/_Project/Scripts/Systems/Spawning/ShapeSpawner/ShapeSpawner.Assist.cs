using UnityEngine;

public partial class ShapeSpawner
{
    private void TickAdaptiveAssist()
    {
        // Simple dynamic: raise assist when no moves, lower when on a streak
        float target = assistLevel;
        if (!HasAnyValidMove())
        {
            target = Mathf.Min(maxAssist, Mathf.Max(assistLevel, 0.8f));
        }
        else
        {
            // Mild decay with streaks
            float decay = Mathf.Clamp01(setsClearedStreak * 0.05f);
            target = Mathf.Lerp(maxAssist, minAssist, decay);
        }
        assistLevel = Mathf.MoveTowards(assistLevel, target, Time.deltaTime * 0.5f);
    }
}
