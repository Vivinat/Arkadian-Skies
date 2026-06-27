using UnityEngine;

public class BattleUnit
{
    public CharacterData data;
    public BattlePosition position;
    public BattleSide side;

    public float currentHP;
    public float currentMana;
    public float attackGauge; // fills up based on attackSpeed, triggers auto-attack at 1.0
    public int autoAttackCount; // total auto-attacks landed, used by EveryNAutoAttacks triggers

    public bool IsAlive => currentHP > 0f;

    public BattleUnit(CharacterData data, BattlePosition position, BattleSide side)
    {
        this.data = data;
        this.position = position;
        this.side = side;
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
}