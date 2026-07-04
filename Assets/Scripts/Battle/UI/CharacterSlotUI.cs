[System.Serializable]
public class CharacterSlotUI
{
    public TurnBarView turnBar;
    public HealthBarView healthBar;
    public ResourceBarView resourceBar;

    public void Bind(BattleUnit unit)
    {
        turnBar.Bind(unit);
        healthBar.Bind(unit);
        resourceBar.Bind(unit);
    }
}