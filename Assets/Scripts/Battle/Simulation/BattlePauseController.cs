using System;
using UnityEngine;

public class BattlePauseController : MonoBehaviour
{
    public float secondsPerPause = 5f;

    public int AvailablePauses { get; private set; } = 0;
    public bool IsPaused { get; private set; } = false;

    public event Action OnPauseStarted;
    public event Action OnPauseEnded;

    float pauseTimer = 0f;

    void Update()
    {
        if (IsPaused) return;

        pauseTimer += Time.deltaTime;
        if (pauseTimer >= secondsPerPause)
        {
            pauseTimer -= secondsPerPause;
            AvailablePauses++;
        }
    }

    // Called by the UI (e.g. a "Pause" button) when the player wants to equip/unequip items or use a global item
    public bool RequestPause()
    {
        if (IsPaused || AvailablePauses <= 0) return false;

        AvailablePauses--;
        IsPaused = true;
        OnPauseStarted?.Invoke();
        return true;
    }

    public void ResumeBattle()
    {
        if (!IsPaused) return;

        IsPaused = false;
        OnPauseEnded?.Invoke();
    }
}