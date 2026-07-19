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

    // Pausing requires at least one charge but does not consume it - charges are only
    // spent by actions (equip, unequip, global item use)
    public bool RequestPause()
    {
        if (IsPaused || AvailablePauses <= 0) return false;

        IsPaused = true;
        OnPauseStarted?.Invoke();
        return true;
    }

    public bool TrySpendPause()
    {
        if (AvailablePauses <= 0) return false;

        AvailablePauses--;
        return true;
    }

    // Lets UI pieces (equip slots, item panels) surface a shared "no charges" warning
    public event Action<string> OnChargeDenied;
    public void NotifyChargeDenied(string reason) => OnChargeDenied?.Invoke(reason);

    public void ResumeBattle()
    {
        if (!IsPaused) return;

        IsPaused = false;
        OnPauseEnded?.Invoke();
    }
}