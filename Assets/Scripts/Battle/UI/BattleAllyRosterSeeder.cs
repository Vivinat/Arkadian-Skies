using UnityEngine;

// Grants roster ownership of the ally composition at battle start, so pause-time
// equipping passes the ownership guard on EquippedItemSlotUI.
public class BattleAllyRosterSeeder : MonoBehaviour
{
    public BattleSimulator simulator;
    public PlayerRoster roster;

    void Start()
    {
        foreach (SlotAssignment assignment in simulator.allyComposition)
            if (assignment.character != null && !roster.Owns(assignment.character))
                roster.AddToBench(assignment.character);
    }
}
