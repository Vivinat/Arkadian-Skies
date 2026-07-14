using UnityEngine;

public class BattleUnit
{
    public CharacterData data;
    public BattlePosition position;
    public BattleSide side;
    public int slotIndex;
    public int level;

    // This unit's stats at its current level, computed once at spawn (level is fixed for the battle's duration).
    CharacterStats baseStats;

    public float currentHP;
    public float currentMana;
    public float attackGauge;
    public int autoAttackCount;

    public float defMultiplier = 1f;
    public float mdefMultiplier = 1f;
    float debuffTimeRemaining = 0f;

    // Permanent per-battle stat bonuses (e.g. Euphrosyne's AP gain on kill). Additive on top of the
    // leveled base stats, never written back to the shared asset itself.
    public float bonusAD = 0f;
    public float bonusAP = 0f;

    public bool isStunned = false;
    float stunTimeRemaining = 0f;

    // Severe Wounds / heal block: always a timed duration, same shape as the DEF/MDEF debuff below.
    public bool isHealBlocked = false;
    float healBlockTimeRemaining = 0f;

    public bool IsAlive => currentHP > 0f;
    public float EffectiveMaxHP => baseStats.maxHP;
    public float EffectiveDEF => baseStats.DEF * defMultiplier;
    public float EffectiveMDEF => baseStats.MDEF * mdefMultiplier;
    public float EffectiveAD => baseStats.AD + bonusAD;
    public float EffectiveAP => baseStats.AP + bonusAP;
    public float EffectiveAttackSpeed => baseStats.attackSpeed;
    public float EffectiveCritChance => baseStats.critChance;
    public float EffectiveCritDamage => baseStats.critDamage;
    public float EffectiveManaPerSecond => baseStats.manaPerSecond;

    public BattleUnit(CharacterData data, BattlePosition position, BattleSide side, int slotIndex = 0, int level = 1)
    {
        this.data = data;
        this.position = position;
        this.side = side;
        this.slotIndex = slotIndex;
        this.level = Mathf.Max(level, 1);
        baseStats = LevelUpCalculator.GetStatsAtLevel(data, this.level);

        currentHP = baseStats.maxHP;
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
        if (isHealBlocked) return;

        amount = Mathf.Max(amount, 0f);
        currentHP = Mathf.Min(currentHP + amount, EffectiveMaxHP);
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

    // Reapplying heal block only extends the remaining duration, never shortens it.
    public void ApplyHealBlock(float duration)
    {
        isHealBlocked = true;
        healBlockTimeRemaining = Mathf.Max(healBlockTimeRemaining, duration);
    }

    public void TickHealBlock(float deltaTime)
    {
        if (!isHealBlocked) return;
        healBlockTimeRemaining -= deltaTime;
        if (healBlockTimeRemaining <= 0f)
        {
            isHealBlocked = false;
            healBlockTimeRemaining = 0f;
        }
    }

    // only Mana-triggered bars reach this - stack mode and reactive triggers are filtered out earlier in ResourceBarView
    public float GetResourceProgress()
    {
        return currentMana / 100f;
    }
}