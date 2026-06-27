using UnityEngine;

public static class DamageCalculator
{
    const float MinDamagePercent = 0.10f;
    const float DamageVariance = 0.10f;

    // applies defense reduction (skipped for True damage), then enforces a damage floor and random variance
    public static float CalculateFinalDamage(float rawDamage, float defense, DamageType damageType)
    {
        float damage;

        if (damageType == DamageType.True)
        {
            damage = rawDamage;
        }
        else
        {
            float reduced = rawDamage - defense;
            float minDamage = rawDamage * MinDamagePercent;
            damage = Mathf.Max(reduced, minDamage);
        }

        float variance = Random.Range(1f - DamageVariance, 1f + DamageVariance);
        return damage * variance;
    }
}