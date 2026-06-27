using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BattleLog
{
    public static string LabelOf(BattleUnit unit)
    {
        string side = unit.side == BattleSide.Ally ? "Ally" : "Enemy";
        string pos = unit.position == BattlePosition.Frontline ? "Front" : "Back";
        return $"{side}-{pos}-{unit.data.characterName}";
    }
}