using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One row inside the Character Info Panel: an ability's icon + name, both wired to show the
// same tooltip on hover. Two of these live in the panel prefab - one for Frontline, one for Backline.
public class AbilityRowUI : MonoBehaviour
{
    public TMP_Text positionLabel; // "Frontline" / "Backline"
    public Image iconImage;
    public AbilityTooltipTrigger iconTooltipTrigger;
    public GameObject activeMarker; // optional: shown when this is the ability currently in use

    // Champion sits in this row's battle position, so this is the ability it actually uses
    public void SetActiveMarker(bool active)
    {
        if (activeMarker != null) activeMarker.SetActive(active);
    }

    public void Bind(Ability ability, string positionText)
    {
        if (positionLabel != null) positionLabel.text = positionText;

        bool hasAbility = ability != null;
        gameObject.SetActive(hasAbility);
        if (!hasAbility) return;

        iconImage.sprite = ability.icon;
        iconImage.enabled = ability.icon != null;

        iconTooltipTrigger.SetAbility(ability);
    }
}