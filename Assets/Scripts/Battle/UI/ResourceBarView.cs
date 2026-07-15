using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceBarView : MonoBehaviour
{
    public Image fillImage;
    public RectTransform stackContainer;
    public GameObject stackPipPrefab;

    [Tooltip("Optional. Shows the raw stack count next to the pips for a real resource like Luna's Butterflies. Left inactive for modulo counters like Aramor's, which have no real 'count' to show.")]
    public TMP_Text stackCountLabel;

    BattleUnit unit;
    Ability ability;
    bool isStackMode;
    IStackResourceDisplay stackDisplay; // non-null for champions whose pips represent a real resource (e.g. Luna), not a modulo counter
    readonly List<GameObject> pips = new List<GameObject>();

    public void Bind(BattleUnit newUnit)
    {
        unit = newUnit;
        ability = unit != null ? unit.GetAbility() : null;

        bool hasBar = unit != null && ability != null && HasVisibleBar(ability, unit);
        gameObject.SetActive(hasBar);
        if (!hasBar) return;

        stackDisplay = ability.customExecutor as IStackResourceDisplay;
        isStackMode = ability.triggerType == TriggerType.EveryNAutoAttacks || stackDisplay != null;
        fillImage.gameObject.SetActive(!isStackMode);
        stackContainer.gameObject.SetActive(isStackMode);
        if (stackCountLabel != null) stackCountLabel.gameObject.SetActive(isStackMode && stackDisplay != null);

        if (isStackMode)
        {
            int pipCount = stackDisplay != null ? Mathf.Max(stackDisplay.GetMaxStacks(unit), 1) : Mathf.Max(ability.autoAttackInterval - 1, 1);
            RebuildPips(pipCount);
        }

        // seed the real progress immediately instead of waiting for the next Update() - matters for
        // mid-battle rebinds (e.g. Nikkal's reposition), where the unit may already have mana/stacks banked
        Refresh();
    }

    // A stack-resource display (e.g. Luna's Butterflies) always shows its own bar, independent of
    // Mana - that's what lets a champion with a Mana-gated Frontline still show pips on the Backline.
    static bool HasVisibleBar(Ability ability, BattleUnit unit)
    {
        if (ability.customExecutor is IStackResourceDisplay) return true;

        if (ability.triggerType == TriggerType.OnKill) return false;
        if (ability.triggerType == TriggerType.EveryNAutoAttacks) return true;

        bool isReactive = ability.triggerType == TriggerType.OnDamageTaken || ability.triggerType == TriggerType.OnAllySingleTargetAbility;
        if (isReactive) return unit.EffectiveManaPerSecond > 0f; // e.g. Jacobo, Luna's Singularity: bar shows readiness to react

        return true;
    }

    void RebuildPips(int count)
    {
        foreach (GameObject pip in pips) Destroy(pip);
        pips.Clear();

        for (int i = 0; i < count; i++)
            pips.Add(Instantiate(stackPipPrefab, stackContainer));
    }

    void Update()
    {
        if (unit == null) return;

        if (!unit.IsAlive)
        {
            gameObject.SetActive(false);
            return;
        }

        Refresh();
    }

    void Refresh()
    {
        if (isStackMode)
        {
            int filled = stackDisplay != null ? stackDisplay.GetCurrentStacks(unit) : unit.autoAttackCount % Mathf.Max(ability.autoAttackInterval, 1);
            for (int i = 0; i < pips.Count; i++)
                pips[i].SetActive(i < filled);

            if (stackDisplay != null && stackCountLabel != null)
                stackCountLabel.text = filled.ToString();
        }
        else
        {
            fillImage.fillAmount = unit.GetResourceProgress();
        }
    }
}
