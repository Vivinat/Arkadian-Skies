using System;
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
    public DialogueUIController dialogueUI;
    public BattlePauseController pauseController;

    // false lets an intro sequence run first - it calls BeginBattle() when done
    public bool startOnAwake = true;

    public event Action<bool> OnBattleEnded; // true = ally victory; a draw counts as defeat

    BattleGrid allySide;
    BattleGrid enemySide;
    BattleEvents events;
    BattleContext context;
    bool battleOver = false;
    bool battleStarted = false;

    public BattleEvents Events => events;
    public bool HasStarted => battleStarted;
    public bool IsOver => battleOver;

    void Awake()
    {
        events = new BattleEvents();
    }

    // Whoever holds this when the enemy team wipes speaks the Victory Quote.
    BattleUnit lastAllyKiller;

    void Start()
    {
        SetupGrids();
        context = new BattleContext(allySide, enemySide, events);

        if (uiManager != null) uiManager.BindGrids(allySide, enemySide);

        RegisterOnKillAbilities(allySide);
        RegisterOnKillAbilities(enemySide);
        RegisterReactiveAbilities(allySide);
        RegisterReactiveAbilities(enemySide);
        RegisterStunReactiveAbilities(allySide);
        RegisterStunReactiveAbilities(enemySide);
        RegisterDamageTakenReactors(allySide);
        RegisterDamageTakenReactors(enemySide);

        events.OnKill += (killer, victim) =>
        {
            if (killer.side == BattleSide.Ally) lastAllyKiller = killer;
        };

        // some abilities (e.g. Nikkal's Elder's Repositioning) move units between slots mid-battle,
        // so the UI needs to be told to rebind whenever that happens
        events.OnPositionsChanged += () =>
        {
            if (uiManager != null) uiManager.BindGrids(allySide, enemySide);
        };

        if (startOnAwake) BeginBattle();
    }

    public void BeginBattle()
    {
        if (battleStarted) return;
        battleStarted = true;
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

            BattleUnit unit = new BattleUnit(assignment.character, assignment.position, side, assignment.slotIndex, assignment.level);
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

                bool ready = unit.EffectiveManaPerSecond <= 0f || unit.currentMana >= 100f;
                if (!ready) return;

                bool triggered = reactor.OnAllySingleTargetAbility(unit, caster, target, context);
                if (triggered)
                {
                    if (unit.EffectiveManaPerSecond > 0f) unit.currentMana = 0f;

                    // UI-only label: "With Me!" becomes "With Me, Luna!" - names the combo partner
                    string comboLabel = $"{ability.abilityName.TrimEnd('!')}, {caster.data.characterName}!";
                    events.RaiseAbilityUsed(unit, ability, comboLabel);
                }
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

    // interface-driven only (no TriggerType filter) - lets any champion react to taking damage,
    // e.g. Luna banking a Butterfly whenever Papillon is hit.
    void RegisterDamageTakenReactors(BattleGrid grid)
    {
        foreach (BattleUnit unit in grid.GetAllUnits())
        {
            events.OnDamageTaken += (victim, attacker, damage, damageType) =>
            {
                if (!unit.IsAlive || victim != unit) return;

                Ability ability = unit.GetAbility();
                if (ability == null || !(ability.customExecutor is IDamageTakenReactor reactor)) return;

                reactor.OnDamageTaken(unit, attacker, damage, damageType, context);
            };
        }
    }

    IEnumerator BattleLoop()
    {
        float tickInterval = 1f / tickRate;
        while (!battleOver)
        {
            if (pauseController != null && pauseController.IsPaused)
            {
                yield return null;
                continue;
            }

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
            unit.TickHealBlock(deltaTime);

            if (unit.isStunned) continue; // stunned units cannot gain mana, fill their attack gauge or act

            Ability ability = unit.GetAbility();

            unit.AddMana(unit.EffectiveManaPerSecond * deltaTime);
            if (ability != null && ability.customExecutor is IPassiveTicker passiveTicker)
                passiveTicker.TickPassive(unit, deltaTime, context);

            unit.attackGauge += unit.EffectiveAttackSpeed * deltaTime;

            // Reactive/self-managed kits (interceptors, custom auto-attacks, ally reactors) treat
            // full Mana as a readiness gate consumed by their own triggers - the simulator must
            // not fire their (empty) Execute or steal their mana at 100
            bool selfManaged = ability != null &&
                (ability.customExecutor is IDamageInterceptor
                 || ability.customExecutor is IAutoAttackOverride
                 || ability.customExecutor is IAllyAbilityReactor);

            if (ability != null && !selfManaged && ability.triggerType == TriggerType.Mana && unit.currentMana >= 100f)
            {
                ability.Execute(unit, context);
                events.RaiseAbilityUsed(unit, ability);
                unit.currentMana = 0f;
                continue; // skip auto-attack this tick if a mana ability fired
            }

            if (unit.attackGauge >= 1f)
                ResolveAutoAttack(unit, ability);
        }
    }

    // Splits off from the plain auto-attack so a champion's ability can swap in a fully custom
    // basic attack (resource-gated, custom target, custom formula) via IAutoAttackOverride.
    void ResolveAutoAttack(BattleUnit unit, Ability ability)
    {
        if (ability != null && ability.customExecutor is IAutoAttackOverride autoOverride)
        {
            if (!autoOverride.CanAutoAttack(unit))
            {
                unit.attackGauge = 1f; // hold at the ready threshold until the resource is available
                return;
            }

            unit.attackGauge -= 1f;

            BattleUnit target = autoOverride.ResolveAutoAttackTarget(unit, context, GetAutoAttackTarget(unit));
            if (target == null) return;

            unit.autoAttackCount++;
            autoOverride.ExecuteAutoAttack(unit, target, context);
            events.RaiseAutoAttack(unit, target);
            return;
        }

        unit.attackGauge -= 1f;
        PerformAutoAttack(unit, ability);
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
            events.RaiseAbilityUsed(attacker, ability);
            events.RaiseAutoAttack(attacker, target);
            return;
        }

        float rawDamage = 0.25f * attacker.EffectiveAD;

        bool didCrit = CritCalculator.Roll(attacker.EffectiveCritChance);
        if (didCrit) rawDamage *= CritCalculator.GetMultiplier(attacker.EffectiveCritDamage);

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
            {
                Debug.Log("Battle ended in a draw - both teams wiped.");
            }
            else if (!enemyAlive)
            {
                Debug.Log("Battle ended: ALLY TEAM WINS.");
                if (dialogueUI != null && lastAllyKiller != null)
                    dialogueUI.Show(lastAllyKiller.data, ChampionQuoteType.Victory);
            }
            else
            {
                Debug.Log("Battle ended: ENEMY TEAM WINS.");
            }

            OnBattleEnded?.Invoke(!enemyAlive && allyAlive);
        }
    }
}
