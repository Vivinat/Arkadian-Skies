using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HealthBarView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image fillImage; // Image Type = Filled, Fill Method = Horizontal
    public TMP_Text valueLabel; // optional: "current/max", shown while hovered

    BattleUnit unit;
    bool hovered;

    public void Bind(BattleUnit newUnit)
    {
        unit = newUnit;
        gameObject.SetActive(unit != null);
        if (valueLabel != null) valueLabel.text = "";

        // seed from the real HP ratio (not a fixed 1f) so mid-battle rebinds (e.g. Nikkal's reposition) don't flicker
        if (unit != null) fillImage.fillAmount = Mathf.Clamp01(unit.currentHP / unit.EffectiveMaxHP);
    }

    void Update()
    {
        if (unit == null) return;

        if (!unit.IsAlive)
        {
            gameObject.SetActive(false);
            return;
        }

        fillImage.fillAmount = Mathf.Clamp01(unit.currentHP / unit.EffectiveMaxHP);

        if (valueLabel != null)
            valueLabel.text = hovered ? $"{unit.currentHP:0}/{unit.EffectiveMaxHP:0}" : "";
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;

    public void OnPointerExit(PointerEventData eventData) => hovered = false;
}