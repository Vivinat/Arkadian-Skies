using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Victory/defeat overlay. Waits a beat so the victory quote dialogue can be read first.
public class BattleResultUI : MonoBehaviour
{
    public BattleSimulator simulator;
    public GameObject panelRoot;
    public TMP_Text titleLabel;
    public TMP_Text subtitleLabel;
    public Button retryButton;
    public float showDelay = 1.6f;

    static readonly Color VictoryColor = new Color(0.97f, 0.87f, 0.47f);
    static readonly Color DefeatColor = new Color(0.94f, 0.36f, 0.38f);

    void OnEnable()
    {
        simulator.OnBattleEnded += HandleEnded;
        retryButton.onClick.AddListener(Retry);
    }

    void OnDisable()
    {
        simulator.OnBattleEnded -= HandleEnded;
        retryButton.onClick.RemoveListener(Retry);
    }

    void HandleEnded(bool allyWon) => StartCoroutine(ShowRoutine(allyWon));

    IEnumerator ShowRoutine(bool allyWon)
    {
        yield return new WaitForSeconds(showDelay);

        titleLabel.text = allyWon ? "VICTORY" : "DEFEAT";
        titleLabel.color = allyWon ? VictoryColor : DefeatColor;
        subtitleLabel.text = allyWon ? "The enemy squad was wiped out." : "Your squad has fallen.";

        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
    }

    void Retry() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}
