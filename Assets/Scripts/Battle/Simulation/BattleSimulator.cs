using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSimulator : MonoBehaviour
{
    public CharacterData allyCharacterData;
    public CharacterData enemyCharacterData;

    [Tooltip("Ticks per second")]
    public float tickRate = 10f;

    List<BattleUnit> allyTeam = new List<BattleUnit>();
    List<BattleUnit> enemyTeam = new List<BattleUnit>();
    BattleEvents events;
    BattleContext context;
    bool battleOver = false;

    void Start()
    {
        events = new BattleEvents();
        SetupTeams();
        context = new BattleContext(allyTeam, enemyTeam, events);

        RegisterOnKillAbilities(allyTeam);
        RegisterOnKillAbilities(enemyTeam);

        StartCoroutine(BattleLoop());
    }

    void SetupTeams()
    {
        allyTeam.Add(new BattleUnit(allyCharacterData, BattlePosition.Frontline, BattleSide.Ally));
        allyTeam.Add(new BattleUnit(allyCharacterData, BattlePosition.Frontline, BattleSide.Ally));
        allyTeam.Add(new BattleUnit(allyCharacterData, BattlePosition.Backline, BattleSide.Ally));
        allyTeam.Add(new BattleUnit(allyCharacterData, BattlePosition.Backline, BattleSide.Ally));

        enemyTeam.Add(new BattleUnit(enemyCharacterData, BattlePosition.Frontline, BattleSide.Enemy));
        enemyTeam.Add(new BattleUnit(enemyCharacterData, BattlePosition.Frontline, BattleSide.Enemy));
        enemyTeam.Add(new BattleUnit(enemyCharacterData, BattlePosition.Backline, BattleSide.Enemy));
        enemyTeam.Add(new BattleUnit(enemyCharacterData, BattlePosition.Backline, BattleSide.Enemy));
    }

    // OnKill abilities don't get checked every tick - they subscribe to the OnKill event once at battle start
    void RegisterOnKillAbilities(List<BattleUnit> team)
    {
        foreach (BattleUnit unit in team)
        {
            Ability ability = unit.GetAbility();
            if (ability == null || ability.triggerType != TriggerType.OnKill) continue;

            events.OnKill += (killer, victim) =>
            {
                if (!unit.IsAlive) return;
                if (killer != unit) return; // only fires for the unit that actually scored the kill
                ability.Execute(unit, context, victim);
            };
        }
    }

    IEnumerator BattleLoop()
    {
        float tickInterval = 1f / tickRate;
        while (!battleOver)
        {
            Tick(tickInterval);
            yield return new WaitForSeconds(tickInterval);
        }
    }

    void Tick(float deltaTime)
    {
        ProcessTeam(allyTeam, deltaTime);
        ProcessTeam(enemyTeam, deltaTime);

        CheckBattleEnd();
    }

    void ProcessTeam(List<BattleUnit> team, float deltaTime)
    {
        foreach (BattleUnit unit in team)
        {
            if (!unit.IsAlive) continue;

            unit.AddMana(unit.data.manaPerSecond * deltaTime);
            unit.attackGauge += unit.data.attackSpeed * deltaTime;

            Ability ability = unit.GetAbility();

            if (ability != null && ability.triggerType == TriggerType.Mana && unit.currentMana >= 100f)
            {
                ability.Execute(unit, context);
                events.RaiseAbilityUsed(unit);
                unit.currentMana = 0f;
                continue; // skip auto-attack this tick if a mana ability fired
            }

            if (unit.attackGauge >= 1f)
            {
                unit.attackGauge -= 1f;
                PerformAutoAttack(unit, ability);
            }
        }
    }

    void PerformAutoAttack(BattleUnit attacker, Ability ability)
    {
        BattleUnit target = GetAutoAttackTarget(attacker);
        if (target == null) return;

        attacker.autoAttackCount++;

        // EveryNAutoAttacks abilities replace the auto-attack itself on the Nth hit
        if (ability != null && ability.triggerType == TriggerType.EveryNAutoAttacks
            && attacker.autoAttackCount % ability.autoAttackInterval == 0)
        {
            ability.Execute(attacker, context, forcedTarget: target);
            events.RaiseAbilityUsed(attacker);
            events.RaiseAutoAttack(attacker, target);
            return;
        }

        float rawDamage = 0.25f * attacker.data.AD;
        float damage = DamageCalculator.CalculateFinalDamage(rawDamage, target.data.DEF, DamageType.AD);
        target.TakeDamage(damage);

        Debug.Log($"{BattleLog.LabelOf(attacker)} auto-attacks {BattleLog.LabelOf(target)} for {damage:F1} damage. {BattleLog.LabelOf(target)} HP: {target.currentHP:F1}/{target.data.maxHP}");

        events.RaiseAutoAttack(attacker, target);

        if (!target.IsAlive)
        {
            Debug.Log($"{BattleLog.LabelOf(target)} has died.");
            events.RaiseKill(attacker, target);
        }
    }

    BattleUnit GetAutoAttackTarget(BattleUnit attacker)
    {
        List<BattleUnit> enemies = context.GetEnemiesOf(attacker);

        List<BattleUnit> frontline = GetAliveByPosition(enemies, BattlePosition.Frontline);
        if (frontline.Count > 0) return frontline[0];

        List<BattleUnit> backline = GetAliveByPosition(enemies, BattlePosition.Backline);
        if (backline.Count > 0) return backline[0];

        return null;
    }

    List<BattleUnit> GetAliveByPosition(List<BattleUnit> team, BattlePosition position)
    {
        List<BattleUnit> result = new List<BattleUnit>();
        foreach (BattleUnit unit in team)
        {
            if (unit.IsAlive && unit.position == position)
                result.Add(unit);
        }
        return result;
    }

    void CheckBattleEnd()
    {
        if (battleOver) return;

        bool allyAlive = TeamHasSurvivors(allyTeam);
        bool enemyAlive = TeamHasSurvivors(enemyTeam);

        if (!allyAlive || !enemyAlive)
        {
            battleOver = true;
            if (!allyAlive && !enemyAlive)
                Debug.Log("Battle ended in a draw - both teams wiped.");
            else if (!enemyAlive)
                Debug.Log("Battle ended: ALLY TEAM WINS.");
            else
                Debug.Log("Battle ended: ENEMY TEAM WINS.");
        }
    }

    bool TeamHasSurvivors(List<BattleUnit> team)
    {
        foreach (BattleUnit unit in team)
        {
            if (unit.IsAlive) return true;
        }
        return false;
    }
}