using UnityEngine;

public class BattleUnit
{
    public CharacterData data;
    public BattlePosition position;
    public BattleSide side;
    public int slotIndex;

    public float currentHP;
    public float currentMana;
    public float attackGauge;
    public int autoAttackCount;

    public float defMultiplier = 1f;
    public float mdefMultiplier = 1f;
    float debuffTimeRemaining = 0f;

    public bool isStunned = false;
    float stunTimeRemaining = 0f;

    public bool IsAlive => currentHP > 0f;
    public float EffectiveDEF => data.DEF * defMultiplier;
    public float EffectiveMDEF => data.MDEF * mdefMultiplier;

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

    public void Heal(float amount)
    {
        amount = Mathf.Max(amount, 0f);
        currentHP = Mathf.Min(currentHP + amount, data.maxHP);
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

    public void ApplyDefMdefDebuff(float multiplier, float duration)
    {
        defMultiplier = multiplier;
        mdefMultiplier = multiplier;
        debuffTimeRemaining = duration;
    }

    public void TickDebuff(float deltaTime)
    {
        if (debuffTimeRemaining <= 0f) return;
        debuffTimeRemaining -= deltaTime;
        if (debuffTimeRemaining <= 0f)
        {
            defMultiplier = 1f;
            mdefMultiplier = 1f;
            debuffTimeRemaining = 0f;
        }
    }

    // basic stun: blocks mana regen, attack gauge and actions in BattleSimulator while active.
    // ClearStun() lets a reactive passive (e.g. Nikkal's Transmogryphy the Pain) cleanse it early.
    public void SetStunned(float duration)
    {
        isStunned = true;
        stunTimeRemaining = duration;
    }

    public void ClearStun()
    {
        isStunned = false;
        stunTimeRemaining = 0f;
    }

    public void TickStun(float deltaTime)
    {
        if (!isStunned) return;
        stunTimeRemaining -= deltaTime;
        if (stunTimeRemaining <= 0f) ClearStun();
    }

    // only Mana-triggered bars reach this - stack mode and reactive triggers are filtered out earlier in ResourceBarView
    public float GetResourceProgress()
    {
        return currentMana / 100f;
    }
}