using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSimulator : MonoBehaviour
{
    [Header("Composition (assign a character per slot)")]
    public List<SlotAssignment> allyComposition = new List<SlotAssignment>();
    public List<SlotAssignment> enemyComposition = new List<SlotAssignment>();

    [Header("Grid Sizes")]
    public int allyFrontlineSlots = 2;
    public int allyBacklineSlots = 2;
    public int enemyFrontlineSlots = 4;
    public int enemyBacklineSlots = 4;

    [Tooltip("Ticks per second")]
    public float tickRate = 10f;

    BattleGrid allySide;
    BattleGrid enemySide;
    BattleEvents events;
    BattleContext context;
    bool battleOver = false;

    void Start()
    {
        events = new BattleEvents();
        SetupGrids();
        context = new BattleContext(allySide, enemySide, events);

        RegisterOnKillAbilities(allySide);
        RegisterOnKillAbilities(enemySide);

        StartCoroutine(BattleLoop());
    }

    void SetupGrids()
    {
        allySide = new BattleGrid(allyFrontlineSlots, allyBacklineSlots);
        enemySide = new BattleGrid(enemyFrontlineSlots, enemyBacklineSlots);

        PlaceComposition(allySide, allyComposition, BattleSide.Ally);
        PlaceComposition(enemySide, enemyComposition, BattleSide.Enemy);
    }

    void PlaceComposition(BattleGrid grid, List<SlotAssignment> composition, BattleSide side)
    {
        foreach (SlotAssignment assignment in composition)
        {
            if (assignment.character == null) continue;

            BattleUnit unit = new BattleUnit(assignment.character, assignment.position, side, assignment.slotIndex);
            bool placed = grid.PlaceUnit(unit, assignment.position, assignment.slotIndex);

            if (!placed)
                Debug.LogWarning($"Could not place {assignment.character.characterName} ({side} {assignment.position}[{assignment.slotIndex}]) - slot out of range or already occupied.");
        }
    }

    // OnKill abilities don't get checked every tick - they subscribe to the OnKill event once at battle start
    void RegisterOnKillAbilities(BattleGrid grid)
    {
        foreach (BattleUnit unit in grid.GetAllUnits())
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
        ProcessGrid(allySide, deltaTime);
        ProcessGrid(enemySide, deltaTime);

        CheckBattleEnd();
    }

    void ProcessGrid(BattleGrid grid, float deltaTime)
    {
        foreach (BattleUnit unit in grid.GetAllAlive())
        {
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
        BattleGrid enemyGrid = context.GetEnemyGridOf(attacker);

        var frontline = enemyGrid.GetAliveByPosition(BattlePosition.Frontline);
        if (frontline.Count > 0) return frontline[0];

        var backline = enemyGrid.GetAliveByPosition(BattlePosition.Backline);
        if (backline.Count > 0) return backline[0];

        return null;
    }

    void CheckBattleEnd()
    {
        if (battleOver) return;

        bool allyAlive = allySide.HasSurvivors();
        bool enemyAlive = enemySide.HasSurvivors();

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
}