using UnityEngine;
using UnityEngine.UI;

public class TurnBarView : MonoBehaviour
{
    public Image fillImage; // Image Type = Filled, Fill Method = Horizontal

    BattleUnit unit;

    public void Bind(BattleUnit newUnit)
    {
        unit = newUnit;
        gameObject.SetActive(unit != null);

        // seed from the real attack gauge (not a fixed 0f) so mid-battle rebinds don't flicker
        if (unit != null) fillImage.fillAmount = Mathf.Clamp01(unit.attackGauge);
    }

    void Update()
    {
        if (unit == null) return;

        if (!unit.IsAlive)
        {
            gameObject.SetActive(false);
            return;
        }

        fillImage.fillAmount = Mathf.Clamp01(unit.attackGauge);
    }
}