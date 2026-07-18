using UnityEngine;

public class BattleUIManager : MonoBehaviour
{
    [Header("Ally slots (index = slot index in BattleGrid)")]
    public CharacterSlotUI[] allyFrontlineSlots;
    public CharacterSlotUI[] allyBacklineSlots;

    [Header("Enemy slots (index = slot index in BattleGrid)")]
    public CharacterSlotUI[] enemyFrontlineSlots;
    public CharacterSlotUI[] enemyBacklineSlots;

    public void BindGrids(BattleGrid allySide, BattleGrid enemySide)
    {
        BindArray(allyFrontlineSlots, allySide.frontline);
        BindArray(allyBacklineSlots, allySide.backline);
        BindArray(enemyFrontlineSlots, enemySide.frontline);
        BindArray(enemyBacklineSlots, enemySide.backline);
    }

    void BindArray(CharacterSlotUI[] slots, BattleUnit[] units)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            BattleUnit unit = i < units.Length ? units[i] : null;
            slots[i].Bind(unit);
        }
    }

    public BattleUnitView FindView(BattleUnit unit)
    {
        BattleUnitView view = FindIn(allyFrontlineSlots, unit);
        if (view == null) view = FindIn(allyBacklineSlots, unit);
        if (view == null) view = FindIn(enemyFrontlineSlots, unit);
        if (view == null) view = FindIn(enemyBacklineSlots, unit);
        return view;
    }

    static BattleUnitView FindIn(CharacterSlotUI[] slots, BattleUnit unit)
    {
        if (slots == null) return null;
        foreach (CharacterSlotUI slot in slots)
            if (slot.unitView != null && slot.unitView.Unit == unit) return slot.unitView;
        return null;
    }
}