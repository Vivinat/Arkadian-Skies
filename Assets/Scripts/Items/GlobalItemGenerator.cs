#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// Generates/updates one GlobalItemData asset per entry of the ITEMS COMPLETOS doc.
// Safe to re-run: existing assets are updated in place by name.
public static class GlobalItemGenerator
{
    const string OutputFolder = "Assets/Items/Globals";

    class Definition
    {
        public string name;
        public string description;
        public GlobalItemEffectType effect;
        public float power;
        public float duration;

        public Definition(string name, string description, GlobalItemEffectType effect, float power, float duration)
        {
            this.name = name;
            this.description = description;
            this.effect = effect;
            this.power = power;
            this.duration = duration;
        }
    }

    static Definition[] BuildDefinitions() => new[]
    {
        new Definition("Ruthanian Anarchy Protocol",
            "Triggers a rebel assault: deals 100 flat AD damage to all enemies.",
            GlobalItemEffectType.DamageAllEnemies, 100f, 0f),
        new Definition("Conviction",
            "For 6 seconds, all allies gain 25% lifesteal on AD damage.",
            GlobalItemEffectType.TeamLifesteal, 0.25f, 6f),
        new Definition("Delphinium's Mirror Shard",
            "For 3 seconds, allies reflect 50% of AD and AP damage taken back to attackers as AP damage.",
            GlobalItemEffectType.ReflectDamage, 0.5f, 3f),
        new Definition("Blood of the First Pures: Unleashed",
            "All allies instantly regenerate 30% of their max HP and gain 2% max HP regen per second for 7 seconds.",
            GlobalItemEffectType.HealAndRegenAllies, 0.02f, 7f),
        new Definition("Erradicator's Final Verdict",
            "Applies Severe Wounds and Severe Bleeding to all enemies for 10 seconds.",
            GlobalItemEffectType.WoundsAndBleedEnemies, 0.015f, 10f),
        new Definition("The Necromancer's Call",
            "Revives all fallen allies with 40% HP and full resource.",
            GlobalItemEffectType.ReviveFallenAllies, 0.4f, 0f),
        new Definition("Halcyon Dream",
            "Heals all allies for a flat 500 HP.",
            GlobalItemEffectType.HealAllAllies, 500f, 0f),
        new Definition("Schemes of the Viper",
            "All allies double their current and max HP for 8 seconds. Excess HP is lost afterwards. (Not implemented yet)",
            GlobalItemEffectType.DoubleTeamHP, 2f, 8f),
        new Definition("Vial of the Great Sea of Ebony",
            "All enemies are blinded and silenced for 5 seconds. (Not implemented yet)",
            GlobalItemEffectType.BlindAndSilence, 0f, 5f)
    };

    [MenuItem("Autobattler/Items/Generate Global Items")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            Directory.CreateDirectory(OutputFolder);
            AssetDatabase.Refresh();
        }

        int created = 0;
        int updated = 0;

        Definition[] definitions = BuildDefinitions();
        for (int i = 0; i < definitions.Length; i++)
        {
            Definition definition = definitions[i];
            string cleanName = definition.name.Replace(":", "");
            string path = $"{OutputFolder}/{cleanName}.asset";
            GlobalItemData asset = AssetDatabase.LoadAssetAtPath<GlobalItemData>(path);
            bool isNew = asset == null;

            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<GlobalItemData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.itemName = definition.name;
            asset.description = definition.description;
            asset.effectType = definition.effect;
            asset.power = definition.power;
            asset.duration = definition.duration;

            // Placeholder icon, only while no hand-made icon has been assigned
            if (asset.icon == null)
                asset.icon = EnsureIcon(cleanName, (float)i / definitions.Length);

            EditorUtility.SetDirty(asset);

            if (isNew) created++;
            else updated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Global items generated at {OutputFolder}: {created} created, {updated} updated.");
    }

    // Distinct hue-tinted diamond per item, so slots read as icons until real art exists
    static Sprite EnsureIcon(string cleanName, float hue)
    {
        string iconFolder = $"{OutputFolder}/Icons";
        Directory.CreateDirectory(Path.Combine(Application.dataPath, iconFolder.Substring("Assets/".Length)));

        string assetPath = $"{iconFolder}/{cleanName} Icon.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (existing != null) return existing;

        Color color = Color.HSVToRGB(hue, 0.62f, 0.92f);
        Texture2D tex = DiamondTexture(48, color);
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

    static Texture2D DiamondTexture(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px = x + 0.5f - half;
            float py = y + 0.5f - half;
            float d = Mathf.Abs(px) + Mathf.Abs(py) - (half - 2f);
            float alpha = Mathf.Clamp01(0.5f - d);

            // simple top-lit shading plus a darker rim
            float shade = 1f + 0.28f * (py / half);
            Color c = new Color(color.r * shade, color.g * shade, color.b * shade, alpha);
            if (d > -3f) c = new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f, alpha);
            tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return tex;
    }
}
#endif
