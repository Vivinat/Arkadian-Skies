using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourceBarView : MonoBehaviour
{
    public Image fillImage;
    public RectTransform stackContainer;
    public GameObject stackPipPrefab;

    BattleUnit unit;
    Ability ability;
    bool isStackMode;
    readonly List<GameObject> pips = new List<GameObject>();

    public void Bind(BattleUnit newUnit)
    {
        unit = newUnit;
        ability = unit != null ? unit.GetAbility() : null;

        bool hasBar = unit != null && ability != null && HasVisibleBar(ability, unit);
        gameObject.SetActive(hasBar);
        if (!hasBar) return;

        isStackMode = ability.triggerType == TriggerType.EveryNAutoAttacks;
        fillImage.gameObject.SetActive(!isStackMode);
        stackContainer.gameObject.SetActive(isStackMode);

        if (isStackMode) RebuildPips(Mathf.Max(ability.autoAttackInterval - 1, 1));
        else fillImage.fillAmount = 0f;
    }

    static bool HasVisibleBar(Ability ability, BattleUnit unit)
    {
        if (ability.triggerType == TriggerType.OnKill) return false;
        if (ability.triggerType == TriggerType.EveryNAutoAttacks) return true;

        bool isReactive = ability.triggerType == TriggerType.OnDamageTaken || ability.triggerType == TriggerType.OnAllySingleTargetAbility;
        if (isReactive) return unit.data.manaPerSecond > 0f; // e.g. Jacobo: bar shows readiness to react

        return true;
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