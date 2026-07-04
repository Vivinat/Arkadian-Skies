using System.Collections.Generic;

// Static bridge between scenes: the roster/map scene fills these lists before loading
// the Battle scene, so BattleSimulator doesn't need to know where the party came from.
public static class BattleSetup
{
    public static List<SlotAssignment> allyComposition;
    public static List<SlotAssignment> enemyComposition;

    public static void Clear()
    {
        allyComposition = null;
        enemyComposition = null;
    }
}