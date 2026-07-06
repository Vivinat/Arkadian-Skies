using UnityEngine;
using UnityEngine.UI;

public class HealthBarView : MonoBehaviour
{
    public Image fillImage; // Image Type = Filled, Fill Method = Horizontal

    BattleUnit unit;

    public void Bind(BattleUnit newUnit)
    {
        unit = newUnit;
        gameObject.SetActive(unit != null);

        // seed from the real HP ratio (not a fixed 1f) so mid-battle rebinds (e.g. Nikkal's reposition) don't flicker
        if (unit != null) fillImage.fillAmount = Mathf.Clamp01(unit.currentHP / unit.data.maxHP);
    }

    void Update()
    {
        if (unit == null) return;

        if (!unit.IsAlive)
        {
            gameObject.SetActive(false);
            return;
        }

        fillImage.fillAmount = Mathf.Clamp01(unit.currentHP / unit.data.maxHP);
    }
}