using UnityEngine;
using UnityEngine.EventSystems;

// Attach to an ability icon Image and/or its name Text. Both can point at the same Ability -
// whichever one the mouse enters shows the same tooltip.
public class AbilityTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Ability ability;

    public void SetAbility(Ability newAbility)
    {
        ability = newAbility;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ability == null) return;
        AbilityTooltip.Instance?.Show(ability.abilityName, ability.description);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AbilityTooltip.Instance?.Hide();
    }
}