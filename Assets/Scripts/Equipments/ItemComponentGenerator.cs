using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Generates/updates one ItemComponentData asset per row of the ingredient table in
// ITEMS_COMPLETOS.docx. Safe to re-run: existing assets are updated in place by name
// instead of being duplicated.
public static class ItemComponentGenerator
{
    const string OutputFolder = "Assets/Items/Components";

    class ComponentDefinition
    {
        public string name;
        public string ptBrName;
        public StatModifier[] modifiers;

        public ComponentDefinition(string name, string ptBrName, params StatModifier[] modifiers)
        {
            this.name = name;
            this.ptBrName = ptBrName;
            this.modifiers = modifiers;
        }
    }

    static StatModifier Flat(StatType type, float value)
    {
        return new StatModifier { statType = type, modifierType = ModifierType.Flat, value = value };
    }

    static StatModifier Percent(StatType type, float value)
    {
        return new StatModifier { statType = type, modifierType = ModifierType.PercentOfBase, value = value };
    }

    static List<ComponentDefinition> BuildDefinitions()
    {
        return new List<ComponentDefinition>
        {
            new ComponentDefinition("Dagger", "Adaga", Flat(StatType.AD, 5)),
            new ComponentDefinition("Long Sword", "Espada Longa", Flat(StatType.AD, 10)),
            new ComponentDefinition("Battle Axe", "Machado de Guerra", Flat(StatType.AD, 15)),
            new ComponentDefinition("Long Bow", "Arco Longo", Percent(StatType.AttackSpeed, 10)),
            new ComponentDefinition("Swift Knife", "Navalha Ligeira", Percent(StatType.AttackSpeed, 15)),
            new ComponentDefinition("Staff", "Cajado", Flat(StatType.AP, 10)),
            new ComponentDefinition("Arcane Orb", "Orbe Arcano", Flat(StatType.AP, 15)),
            new ComponentDefinition("Grimoire", "Grimório", Flat(StatType.AP, 20)),
            new ComponentDefinition("Arcanite Fragment", "Fragmento de Arcanita", Flat(StatType.ManaPerSecond, 3)),
            new ComponentDefinition("Arcanite Battery", "Bateria de Arcanita", Flat(StatType.ManaPerSecond, 2)),
            new ComponentDefinition("Benediction", "Benção", Flat(StatType.ManaPerSecond, 5)),
            new ComponentDefinition("Light Armor", "Armadura Leve", Flat(StatType.MaxHP, 100)),
            new ComponentDefinition("Medium Armor", "Armadura Média", Flat(StatType.MaxHP, 150)),
            new ComponentDefinition("Heavy Armor", "Armadura Pesada", Flat(StatType.MaxHP, 200)),
            new ComponentDefinition("Iron Breastplate", "Peitoral de Ferro", Flat(StatType.DEF, 20)),
            new ComponentDefinition("Runic Mantle", "Manto Rúnico", Flat(StatType.MDEF, 20)),
            // CritChance is a 0-1 fraction (matches CharacterData.critChance); CritDamage has no
            // base stat to be "a percent of", so it's stored directly in percentage points.
            new ComponentDefinition("Sparring Gloves", "Luvas de Duelo",
                Flat(StatType.CritChance, 0.05f), Flat(StatType.CritDamage, 10f)),
            new ComponentDefinition("Blood of the Ancients", "Sangue dos Antigos",
                Percent(StatType.MaxHP, 10), Percent(StatType.DEF, 10), Percent(StatType.MDEF, 10)),
            new ComponentDefinition("Wreath of Yore", "Grinalda de Outrora", Percent(StatType.AD, 10)),
            new ComponentDefinition("Broken Oath", "Juramento Quebrado", Flat(StatType.DEF, 10), Flat(StatType.MDEF, 10)),
        };
    }

    [MenuItem("Autobattler/Items/Generate Item Components")]
    public static void GenerateComponents()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            Directory.CreateDirectory(OutputFolder);
            AssetDatabase.Refresh();
        }

        int created = 0;
        int updated = 0;

        foreach (ComponentDefinition definition in BuildDefinitions())
        {
            string path = $"{OutputFolder}/{definition.name}.asset";
            ItemComponentData asset = AssetDatabase.LoadAssetAtPath<ItemComponentData>(path);
            bool isNew = asset == null;

            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<ItemComponentData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.itemName = definition.name;
            asset.description = $"({definition.ptBrName})";
            asset.modifiers = new List<StatModifier>(definition.modifiers);
            EditorUtility.SetDirty(asset);

            if (isNew) created++;
            else updated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Item components generated at {OutputFolder}: {created} created, {updated} updated.");
    }
}