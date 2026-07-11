public enum ModifierType
{
    Flat,        // value is added directly, same units as the stat itself
    PercentOfBase // value is a percentage point (10 = +10%), applied once over base+flat total
}