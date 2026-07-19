#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Generates the complete equipment items from the ITEMS COMPLETOS doc: stats, passive
// descriptions and the two-component recipe each one is forged from, plus the recipe
// book asset the bank uses to resolve combinations. Passive EFFECTS are not implemented
// here - they land in battle code item by item.
public static class CompleteItemGenerator
{
    const string ComponentsFolder = "Assets/Items/Components";
    const string OutputFolder = "Assets/Items/Complete";
    const string RecipeBookPath = "Assets/Items/ItemRecipeBook.asset";

    class Definition
    {
        public string name;
        public string passive;
        public string componentA;
        public string componentB;
        public StatModifier[] modifiers;

        public Definition(string name, string passive, string componentA, string componentB, params StatModifier[] modifiers)
        {
            this.name = name;
            this.passive = passive;
            this.componentA = componentA;
            this.componentB = componentB;
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

    // "Sangue Amaldiçoado" in the doc's recipe table maps to the Blood of the Ancients component
    static List<Definition> BuildDefinitions() => new List<Definition>
    {
        // Crit chance
        new Definition("Finésse", "", "Long Sword", "Sparring Gloves",
            Flat(StatType.AD, 15), Flat(StatType.CritChance, 0.34f)),
        new Definition("Karkrinos", "", "Swift Knife", "Sparring Gloves",
            Percent(StatType.AttackSpeed, 15), Flat(StatType.CritChance, 0.34f)),
        new Definition("Apex Predator", "Passive: critical hits grant 15% lifesteal.", "Sparring Gloves", "Sparring Gloves",
            Flat(StatType.CritChance, 0.34f)),
        new Definition("Vanguard's Charge", "Passive: at battle start, the ally directly in front of or behind the wearer gains +15% Crit Chance and +15% Attack Speed for the first 10 seconds.", "Medium Armor", "Sparring Gloves",
            Flat(StatType.MaxHP, 200), Flat(StatType.CritChance, 0.20f)),

        // Crit damage
        new Definition("Brungrildr", "", "Long Sword", "Long Sword",
            Flat(StatType.CritChance, 0.10f), Flat(StatType.CritDamage, 50), Flat(StatType.AD, 10)),
        new Definition("Axley's Bow", "No crit chance of its own - relies on other sources.", "Battle Axe", "Sparring Gloves",
            Flat(StatType.AD, 15), Flat(StatType.CritDamage, 50)),
        new Definition("Ruthanian Regards", "", "Dagger", "Sparring Gloves",
            Flat(StatType.CritChance, 0.25f), Flat(StatType.CritDamage, 25), Flat(StatType.AD, 5)),

        // Ability crits
        new Definition("Crystal of Consensus", "Passive: AP-damage abilities can critically strike, using the wearer's Crit Chance and Crit Damage.", "Grimoire", "Sparring Gloves",
            Flat(StatType.AP, 25), Flat(StatType.CritChance, 0.15f)),
        new Definition("Shard of Hubris", "Passive: AD-damage abilities can critically strike, using the wearer's Crit Chance and Crit Damage.", "Battle Axe", "Long Sword",
            Flat(StatType.AD, 25), Flat(StatType.CritChance, 0.15f)),

        // Health regen
        new Definition("Blood of the First Pures", "Passive: regenerates 1.5% max HP per second.", "Heavy Armor", "Medium Armor",
            Flat(StatType.MaxHP, 350)),
        new Definition("Savage Heart", "Passive: 12% lifesteal on auto-attacks.", "Battle Axe", "Dagger",
            Flat(StatType.AD, 20)),
        new Definition("Flowery Hemagedon", "Passive: reflects 18% of damage taken back to the attacker.", "Heavy Armor", "Heavy Armor",
            Flat(StatType.MaxHP, 400)),

        // Mana regen
        new Definition("Rukhanian Codex", "", "Grimoire", "Grimoire",
            Flat(StatType.AP, 40), Flat(StatType.ManaPerSecond, 3)),
        new Definition("Mecanomancy's Core", "", "Medium Armor", "Benediction",
            Flat(StatType.MaxHP, 150), Flat(StatType.ManaPerSecond, 5)),
        new Definition("Yorebringer", "Passive: dealing ability damage instantly restores 5 extra Mana.", "Grimoire", "Arcanite Battery",
            Flat(StatType.AP, 20), Flat(StatType.ManaPerSecond, 2)),
        new Definition("Miraclemaker's Intervention", "Passive: while at 100% HP, the item's mana regen is doubled (+6/s). Taking damage reverts it.", "Arcane Orb", "Arcane Orb",
            Flat(StatType.AP, 30), Flat(StatType.ManaPerSecond, 3)),

        // Attributes
        new Definition("Iron Grip of the Tyrant", "", "Light Armor", "Iron Breastplate",
            Flat(StatType.MaxHP, 100), Flat(StatType.DEF, 40)),
        new Definition("Delphiniam Cursed Armor", "", "Blood of the Ancients", "Blood of the Ancients",
            Percent(StatType.DEF, 25)),
        new Definition("Cthonian War Mantle", "", "Runic Mantle", "Runic Mantle",
            Flat(StatType.MDEF, 35)),
        new Definition("Embrace of Consensus", "", "Light Armor", "Blood of the Ancients",
            Flat(StatType.MaxHP, 100), Percent(StatType.MDEF, 20)),
        new Definition("Titanic Blood", "", "Medium Armor", "Blood of the Ancients",
            Percent(StatType.MaxHP, 20)),

        // Attack speed
        new Definition("Theocracy's War Horn", "", "Long Bow", "Swift Knife",
            Percent(StatType.AttackSpeed, 25)),
        new Definition("Lightning Swiftblade", "", "Battle Axe", "Swift Knife",
            Flat(StatType.AD, 15), Percent(StatType.AttackSpeed, 20)),
        new Definition("Casus Belli", "Passive (aura): the ally directly behind the wearer gains +10% Attack Speed.", "Swift Knife", "Dagger",
            Percent(StatType.AttackSpeed, 15)),
        new Definition("Twin Catastrophe", "Passive (Cyclic Strike): every 3 consecutive basic attacks on the same target deal bonus physical damage equal to 3% of the target's max HP.", "Long Bow", "Long Sword",
            Percent(StatType.AttackSpeed, 15), Flat(StatType.AD, 10)),

        // Unique / special effects
        new Definition("Helleborian Warpath", "Passive: once per battle, on reaching 0 HP, revive with 30% max HP instead of dying.", "Heavy Armor", "Iron Breastplate",
            Flat(StatType.MaxHP, 200), Flat(StatType.DEF, 30)),
        new Definition("Amarylian Ricin Blood", "Passive: on death, explodes and heals all allies for 20% of the wearer's max HP.", "Medium Armor", "Runic Mantle",
            Flat(StatType.MaxHP, 150), Flat(StatType.MDEF, 20)),
        new Definition("Radioactive Shard of the Erradicator", "Passive: attacks and abilities apply Severe Wounds for 5 seconds (target cannot be healed). Does not stack.", "Staff", "Grimoire",
            Flat(StatType.AP, 30)),
        new Definition("Bloodletter", "Passive: attacks apply Severe Bleeding, dealing continuous damage for the rest of the fight. Does not stack.", "Battle Axe", "Battle Axe",
            Flat(StatType.AD, 30)),
        new Definition("Kenominatic Gehennan", "", "Wreath of Yore", "Wreath of Yore",
            Percent(StatType.AD, 30)),
        new Definition("Plate of the Challenger", "Passive: each attack received grants +2% DEF and MDEF for the rest of the battle (up to 10 stacks).", "Iron Breastplate", "Runic Mantle",
            Flat(StatType.DEF, 35), Flat(StatType.MDEF, 35)),
        new Definition("Halfstep", "Passive: once per battle, on receiving a killing blow, become untargetable for 2 seconds instead of dying.", "Broken Oath", "Broken Oath",
            Flat(StatType.DEF, 25), Flat(StatType.MDEF, 25)),
        new Definition("Pity", "Instantly kills the wearer after 1 second.", "Dagger", "Dagger"),
        new Definition("The Revelation", "Sets the wearer's Attack Speed to zero.", "Long Bow", "Long Bow"),
        new Definition("EMP Gauntlet", "Passive: basic attacks deal 200% bonus damage against shields.", "Swift Knife", "Swift Knife",
            Percent(StatType.AttackSpeed, 25)),
        new Definition("Fleeting Dream", "Passive: immune to crowd control (Stun, Confusion, Taunt).", "Runic Mantle", "Dagger",
            Flat(StatType.MDEF, 20)),
        new Definition("Idol of Martyrdom", "Passive: at battle start, summons a Wooden Effigy (300 HP, 0 DEF, does not attack) in each empty party slot to soak hits.", "Medium Armor", "Dagger",
            Flat(StatType.MaxHP, 150), Flat(StatType.DEF, 10)),
        new Definition("Shield of Ages", "Passive: whenever the wearer takes True Damage, reduce it by 20% and gain a shield equal to 5% max HP.", "Light Armor", "Medium Armor",
            Flat(StatType.MaxHP, 200)),
        new Definition("Ruinmaker", "Passive: healing received beyond max HP converts into a shield that lasts the whole fight (cap: 50% max HP).", "Blood of the Ancients", "Heavy Armor",
            Percent(StatType.MaxHP, 20)),
        new Definition("Galvanized Teoricinae", "Passive: at battle start, shields all allies for 20% of the wearer's max HP.", "Medium Armor", "Medium Armor",
            Flat(StatType.MaxHP, 300))
    };

    [MenuItem("Autobattler/Items/Generate Complete Items")]
    public static void Generate()
    {
        // components must exist first - recipes reference them directly
        ItemComponentGenerator.GenerateComponents();

        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            Directory.CreateDirectory(OutputFolder);
            AssetDatabase.Refresh();
        }

        int created = 0;
        int updated = 0;
        int missingComponents = 0;

        List<Definition> definitions = BuildDefinitions();
        List<ItemComponentData> completeItems = new List<ItemComponentData>();

        for (int i = 0; i < definitions.Count; i++)
        {
            Definition definition = definitions[i];
            ItemComponentData componentA = LoadComponent(definition.componentA);
            ItemComponentData componentB = LoadComponent(definition.componentB);
            if (componentA == null || componentB == null)
            {
                Debug.LogWarning($"Complete item '{definition.name}' skipped - missing component '{definition.componentA}' or '{definition.componentB}'.");
                missingComponents++;
                continue;
            }

            string path = $"{OutputFolder}/{definition.name.Replace(":", "")}.asset";
            ItemComponentData asset = AssetDatabase.LoadAssetAtPath<ItemComponentData>(path);
            bool isNew = asset == null;

            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<ItemComponentData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.itemName = definition.name;
            asset.description = definition.passive;
            asset.modifiers = new List<StatModifier>(definition.modifiers);
            asset.isCompleteItem = true;
            asset.recipeComponentA = componentA;
            asset.recipeComponentB = componentB;

            if (asset.icon == null)
                asset.icon = EnsureIcon(definition.name.Replace(":", ""), (float)i / definitions.Count);

            EditorUtility.SetDirty(asset);
            completeItems.Add(asset);

            if (isNew) created++;
            else updated++;
        }

        ItemRecipeBook book = AssetDatabase.LoadAssetAtPath<ItemRecipeBook>(RecipeBookPath);
        if (book == null)
        {
            book = ScriptableObject.CreateInstance<ItemRecipeBook>();
            AssetDatabase.CreateAsset(book, RecipeBookPath);
        }
        book.completeItems = completeItems;
        EditorUtility.SetDirty(book);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Complete items generated at {OutputFolder}: {created} created, {updated} updated, {missingComponents} skipped. Recipe book: {RecipeBookPath}");
    }

    static ItemComponentData LoadComponent(string componentName)
    {
        return AssetDatabase.LoadAssetAtPath<ItemComponentData>($"{ComponentsFolder}/{componentName}.asset");
    }

    // Hue-tinted forged-gem placeholder (square, to read differently from the global
    // items' diamonds), only used while no hand-made icon is assigned
    static Sprite EnsureIcon(string cleanName, float hue)
    {
        string iconFolder = $"{OutputFolder}/Icons";
        Directory.CreateDirectory(Path.Combine(Application.dataPath, iconFolder.Substring("Assets/".Length)));

        string assetPath = $"{iconFolder}/{cleanName} Icon.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (existing != null) return existing;

        Color color = Color.HSVToRGB(hue, 0.58f, 0.95f);
        Texture2D tex = GemTexture(48, color);
        File.WriteAllBytes(Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length)), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    static Texture2D GemTexture(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float margin = 6f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool inside = x >= margin && x < size - margin && y >= margin && y < size - margin;
            if (!inside)
            {
                tex.SetPixel(x, y, Color.clear);
                continue;
            }

            bool rim = x < margin + 3 || x >= size - margin - 3 || y < margin + 3 || y >= size - margin - 3;
            float shade = 1f + 0.28f * ((y - size * 0.5f) / size);
            Color c = rim
                ? new Color(color.r * 0.4f, color.g * 0.4f, color.b * 0.4f, 1f)
                : new Color(color.r * shade, color.g * shade, color.b * shade, 1f);
            tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return tex;
    }
}
#endif
