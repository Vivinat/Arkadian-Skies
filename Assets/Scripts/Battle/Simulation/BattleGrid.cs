using System.Collections.Generic;

public class BattleGrid
{
    public BattleUnit[] frontline;
    public BattleUnit[] backline;

    public BattleGrid(int frontlineSize, int backlineSize)
    {
        frontline = new BattleUnit[frontlineSize];
        backline = new BattleUnit[backlineSize];
    }

    BattleUnit[] ArrayFor(BattlePosition position)
    {
        return position == BattlePosition.Frontline ? frontline : backline;
    }

    public bool PlaceUnit(BattleUnit unit, BattlePosition position, int slotIndex)
    {
        BattleUnit[] array = ArrayFor(position);
        if (slotIndex < 0 || slotIndex >= array.Length) return false;
        if (array[slotIndex] != null) return false; // slot occupied

        unit.position = position;
        unit.slotIndex = slotIndex;
        array[slotIndex] = unit;
        return true;
    }

    public void RemoveUnit(BattleUnit unit)
    {
        BattleUnit[] array = ArrayFor(unit.position);
        if (unit.slotIndex >= 0 && unit.slotIndex < array.Length && array[unit.slotIndex] == unit)
            array[unit.slotIndex] = null;
    }

    public BattleUnit GetUnitAt(BattlePosition position, int slotIndex)
    {
        BattleUnit[] array = ArrayFor(position);
        if (slotIndex < 0 || slotIndex >= array.Length) return null;
        return array[slotIndex];
    }

    public List<BattleUnit> GetAliveByPosition(BattlePosition position)
    {
        List<BattleUnit> result = new List<BattleUnit>();
        foreach (BattleUnit unit in ArrayFor(position))
        {
            if (unit != null && unit.IsAlive) result.Add(unit);
        }
        return result;
    }

    public List<BattleUnit> GetAllAlive()
    {
        List<BattleUnit> result = new List<BattleUnit>();
        result.AddRange(GetAliveByPosition(BattlePosition.Frontline));
        result.AddRange(GetAliveByPosition(BattlePosition.Backline));
        return result;
    }

    public List<BattleUnit> GetAllUnits()
    {
        List<BattleUnit> result = new List<BattleUnit>();
        foreach (BattleUnit unit in frontline) if (unit != null) result.Add(unit);
        foreach (BattleUnit unit in backline) if (unit != null) result.Add(unit);
        return result;
    }

    public bool HasSurvivors()
    {
        foreach (BattleUnit unit in frontline) if (unit != null && unit.IsAlive) return true;
        foreach (BattleUnit unit in backline) if (unit != null && unit.IsAlive) return true;
        return false;
    }

    // only meaningful within the same grid/team: backline unit looks up the frontline unit at the same slot index
    public BattleUnit GetMirroredFrontline(BattleUnit backlineUnit)
    {
        if (backlineUnit.position != BattlePosition.Backline) return null;
        return GetUnitAt(BattlePosition.Frontline, backlineUnit.slotIndex);
    }
}