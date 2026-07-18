[System.Serializable]
public class CharacterSlotUI
{
    public TurnBarView turnBar;
    public HealthBarView healthBar;
    public ResourceBarView resourceBar;
    public BattleUnitView unitView; // optional

    public void Bind(BattleUnit unit)
    {
        turnBar.Bind(unit);
        healthBar.Bind(unit);
        resourceBar.Bind(unit);
        if (unitView != null) unitView.Bind(unit);
    }
}