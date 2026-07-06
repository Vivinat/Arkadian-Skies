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

    public float tickRate = 10f;

    public BattleUIManager uiManager;

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

        if (uiManager != null) uiManager.BindGrids(allySide, enemySide);

        RegisterOnKillAbilities(allySide);
        RegisterOnKillAbilities(enemySide);
        RegisterReactiveAbilities(allySide);
        RegisterReactiveAbilities(enemySide);
        RegisterStunReactiveAbilities(allySide);
        RegisterStunReactiveAbilities(enemySide);

        // some abilities (e.g. Nikkal's Elder's Repositioning) move units between slots mid-battle,
        // so the UI needs to be told to rebind whenever that happens
        events.OnPositionsChanged += () =>
        {
            if (uiManager != null) uiManager.BindGrids(allySide, enemySide);
        };

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

    void RegisterOnKillAbilities(BattleGrid grid)
    {
        foreach (BattleUnit unit in grid.GetAllUnits())
        {
            Ability ability = unit.GetAbility();
            if (ability == null || ability.triggerType != TriggerType.OnKill) continue;

            events.OnKill += (killer, victim) =>
            {
                if (!unit.IsAlive) return;
                if (killer != unit) return;
                ability.Execute(unit, context, victim);
            };
        }
    }

    void RegisterReactiveAbilities(BattleGrid grid)
    {
        foreach (BattleUnit unit in grid.GetAllUnits())
        {
            Ability ability = unit.GetAbility();
            if (ability == null || ability.triggerType != TriggerType.OnAllySingleTargetAbility) continue;
            if (!(ability.customExecutor is IAllyAbilityReactor reactor)) continue;

            events.OnBeforeSingleTargetAbility += (caster, target) =>
            {
                if (!unit.IsAlive || caster == unit || caster.side != unit.side) return;

                bool ready = unit.data.manaPerSecond <= 0f || unit.currentMana >= 100f;
                if (!ready) return;

                bool triggered = reactor.OnAllySingleTargetAbility(unit, caster, target, context);
                if (triggered && unit.data.manaPerSecond > 0f) unit.currentMana = 0f;
            };
        }
    }

    // fetches unit.GetAbility() fresh on every event instead of once at registration time, because a unit like
    // Nikkal can change position (and therefore which ability/passive is active) mid-battle
    void RegisterStunReactiveAbilities(BattleGrid grid)
    {
        foreach (BattleUnit unit in grid.GetAllUnits())
        {
            events.OnUnitStunned += (stunnedUnit) =>
            {
                if (!unit.IsAlive || stunnedUnit == unit || stunnedUnit.side != unit.side) return;

                Ability ability = unit.GetAbility();
                if (ability == null || !(ability.customExecutor is IAllyStunReactor reactor)) return;

                bool triggered = reactor.OnAllyStunned(unit, stunnedUnit, context);
                if (triggered) unit.currentMana = 0f;
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
            unit.TickDebuff(deltaTime);
            unit.TickStun(deltaTime);

            if (unit.isStunned) continue; // stunned units cannot gain mana, fill their attack gauge or act

            unit.AddMana(unit.data.manaPerSecond * deltaTime);
            unit.attackGauge += unit.data.attackSpeed * deltaTime;

            Ability ability = unit.GetAbility();

            if (ability != null && ability.triggerType == TriggerType.Mana && unit.currentMana >= 100f)
            {
                ability.Execute(unit, context);
                events.RaiseAbilityUsed(unit);
                unit.currentMana = 0f;
                continue;
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

        if (ability != null && ability.triggerType == TriggerType.EveryNAutoAttacks
            && attacker.autoAttackCount % ability.autoAttackInterval == 0)
        {
            ability.Execute(attacker, context, forcedTarget: target);
            events.RaiseAbilityUsed(attacker);
            events.RaiseAutoAttack(attacker, target);
            return;
        }

        float rawDamage = 0.25f * attacker.data.AD;
        CombatResolver.ApplyDamage(attacker, target, rawDamage, DamageType.AD, context);
        events.RaiseAutoAttack(attacker, target);
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