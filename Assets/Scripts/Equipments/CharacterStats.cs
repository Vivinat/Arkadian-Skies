// Snapshot of a champion's stats after equipped item modifiers are applied on top of CharacterData.
// Field names/units match CharacterData exactly so this can be used anywhere those numbers are needed.
public class CharacterStats
{
    public float maxHP;
    public float AD;
    public float AP;
    public float attackSpeed;
    public float critChance;
    public float critDamage;
    public float DEF;
    public float MDEF;
    public float manaPerSecond;

    public static CharacterStats FromBase(CharacterData data)
    {
        return new CharacterStats
        {
            maxHP = data.maxHP,
            AD = data.AD,
            AP = data.AP,
            attackSpeed = data.attackSpeed,
            critChance = data.critChance,
            critDamage = 0f,
            DEF = data.DEF,
            MDEF = data.MDEF,
            manaPerSecond = data.manaPerSecond
        };
    }

    public float Get(StatType type)
    {
        switch (type)
        {
            case StatType.MaxHP: return maxHP;
            case StatType.AD: return AD;
            case StatType.AP: return AP;
            case StatType.AttackSpeed: return attackSpeed;
            case StatType.CritChance: return critChance;
            case StatType.CritDamage: return critDamage;
            case StatType.DEF: return DEF;
            case StatType.MDEF: return MDEF;
            case StatType.ManaPerSecond: return manaPerSecond;
            default: return 0f;
        }
    }

    public void Add(StatType type, float amount)
    {
        switch (type)
        {
            case StatType.MaxHP: maxHP += amount; break;
            case StatType.AD: AD += amount; break;
            case StatType.AP: AP += amount; break;
            case StatType.AttackSpeed: attackSpeed += amount; break;
            case StatType.CritChance: critChance += amount; break;
            case StatType.CritDamage: critDamage += amount; break;
            case StatType.DEF: DEF += amount; break;
            case StatType.MDEF: MDEF += amount; break;
            case StatType.ManaPerSecond: manaPerSecond += amount; break;
        }
    }
}