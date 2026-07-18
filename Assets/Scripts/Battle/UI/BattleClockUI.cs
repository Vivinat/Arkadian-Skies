using TMPro;
using UnityEngine;

// Battle timer shown in the top bar. Only counts while the fight is actually running.
public class BattleClockUI : MonoBehaviour
{
    public BattleSimulator simulator;
    public BattlePauseController pauseController;
    public TMP_Text label;

    float elapsed;

    void Update()
    {
        if (simulator.HasStarted && !simulator.IsOver && (pauseController == null || !pauseController.IsPaused))
            elapsed += Time.deltaTime;

        int minutes = (int)(elapsed / 60f);
        int seconds = (int)(elapsed % 60f);
        label.text = $"{minutes}:{seconds:00}";
    }
}
