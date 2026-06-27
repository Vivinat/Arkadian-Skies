using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseEffect : ScriptableObject
{
    public abstract void Execute(BattleUnit caster, List<BattleUnit> targets, BattleContext context);
}