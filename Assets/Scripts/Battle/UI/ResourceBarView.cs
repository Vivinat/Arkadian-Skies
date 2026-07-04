using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourceBarView : MonoBehaviour
{
    [Header("Fill mode - mana-style bars")]
    public Image fillImage; // Image Type = Filled, Fill Method = Horizontal

    [Header("Stack mode - e.g. Aramor's auto-attack counter")]
    public RectTransform stackContainer; // needs a Horizontal Layout Group so pips line up to the right
    public GameObject stackPipPrefab;

    BattleUnit unit;
    Ability ability;
    bool isStackMode;
    readonly List<GameObject> pips = new List<GameObject>();

    public void Bind(BattleUnit newUnit)
    {
        unit = newUnit;
        ability = unit != null ? unit.GetAbility() : null;

        // OnKill abilities are purely reactive (e.g. Aramor's backline kit) - no continuous
        // progress exists to show, so the whole bar just hides for that slot/position
        bool hasBar = unit != null && ability != null && ability.triggerType != TriggerType.OnKill;
        gameObject.SetActive(hasBar);
        if (!hasBar) return;

        isStackMode = ability.triggerType == TriggerType.EveryNAutoAttacks;
        fillImage.gameObject.SetActive(!isStackMode);
        stackContainer.gameObject.SetActive(isStackMode);

        if (isStackMode) RebuildPips(Mathf.Max(ability.autoAttackInterval - 1, 1));
        else fillImage.fillAmount = 0f;
    }

    void RebuildPips(int count)
    {
        foreach (GameObject pip in pips) Destroy(pip);
        pips.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject pip = Instantiate(stackPipPrefab, stackContainer);
            pip.SetActive(false);
            pips.Add(pip);
        }
    }

    void Update()
    {
        if (unit == null) return;

        if (!unit.IsAlive)
        {
            gameObject.SetActive(false);
            return;
        }

        if (isStackMode)
        {
            int interval = Mathf.Max(ability.autoAttackInterval, 1);
            int filled = unit.autoAttackCount % interval;
            for (int i = 0; i < pips.Count; i++)
                pips[i].SetActive(i < filled);
        }
        else
        {
            fillImage.fillAmount = unit.GetResourceProgress();
        }
    }
}