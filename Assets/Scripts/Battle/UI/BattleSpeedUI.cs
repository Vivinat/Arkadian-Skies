using UnityEngine;
using UnityEngine.UI;

// Battle speed control via Time.timeScale: very slow | slow | normal | fast | very fast.
// The selected button is disabled so its disabled tint doubles as the "selected" state.
public class BattleSpeedUI : MonoBehaviour
{
    public Button[] speedButtons; // ordered slowest -> fastest
    public float[] speeds = { 0.25f, 0.5f, 1f, 2f, 4f };
    public int defaultIndex = 2;

    public int CurrentIndex { get; private set; }

    void OnEnable()
    {
        for (int i = 0; i < speedButtons.Length; i++)
        {
            int index = i;
            speedButtons[i].onClick.AddListener(() => Apply(index));
        }
    }

    void OnDisable()
    {
        foreach (Button button in speedButtons)
            button.onClick.RemoveAllListeners();
    }

    void Start() => Apply(defaultIndex);

    public void Apply(int index)
    {
        CurrentIndex = Mathf.Clamp(index, 0, speeds.Length - 1);
        Time.timeScale = speeds[CurrentIndex];

        for (int i = 0; i < speedButtons.Length; i++)
            speedButtons[i].interactable = i != CurrentIndex;
    }

    void OnDestroy() => Time.timeScale = 1f;
}
