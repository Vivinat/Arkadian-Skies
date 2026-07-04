using UnityEngine;

public class BattleUnit
{
    public CharacterData data;
    public BattlePosition position;
    public BattleSide side;
    public int slotIndex; // index within its position array on the grid (e.g. Frontline[0])

    public float currentHP;
    public float currentMana;
    public float attackGauge; // fills up based on attackSpeed, triggers auto-attack at 1.0
    public int autoAttackCount; // total auto-attacks landed, used by EveryNAutoAttacks triggers

    public bool IsAlive => currentHP > 0f;

    public BattleUnit(CharacterData data, BattlePosition position, BattleSide side, int slotIndex = 0)
    {
        this.data = data;
        this.position = position;
        this.side = side;
        this.slotIndex = slotIndex;
        currentHP = data.maxHP;
        currentMana = 0f;
        attackGauge = 0f;
        autoAttackCount = 0;
    }

    public void TakeDamage(float amount)
    {
        amount = Mathf.Max(amount, 0f);
        currentHP -= amount;
        if (currentHP < 0f) currentHP = 0f;
    }

    public void AddMana(float amount)
    {
        currentMana += amount;
        if (currentMana > 100f) currentMana = 100f;
    }

    public Ability GetAbility()
    {
        return position == BattlePosition.Frontline ? data.frontlineAbility : data.backlineAbility;
    }

    // Normalized 0-1 progress for the UI resource bar. Generalizes across trigger types since
    // not every kit uses a 0-100 pool (e.g. Aramor's EveryNAutoAttacks doesn't use mana at all).
    public float GetResourceProgress()
    {
        Ability ability = GetAbility();
        if (ability == null) return 0f;

        switch (ability.triggerType)
        {
            case TriggerType.Mana:
                return Mathf.Clamp01(currentMana / 100f);

            case TriggerType.EveryNAutoAttacks:
                int interval = Mathf.Max(ability.autoAttackInterval, 1);
                return (autoAttackCount % interval) / (float)interval;

            case TriggerType.OnKill:
            default:
                return 0f; // reactive trigger, fires off an event rather than a continuous fill
        }
    }
}