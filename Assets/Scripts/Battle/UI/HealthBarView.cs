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
        if (unit != null) fillImage.fillAmount = 1f;
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