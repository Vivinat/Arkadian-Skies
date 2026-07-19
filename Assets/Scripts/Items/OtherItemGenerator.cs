#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// Generates the four map consumables ("Outros Items") with placeholder icons.
public static class OtherItemGenerator
{
    const string OutputFolder = "Assets/Items/Others";

    class Definition
    {
        public string name;
        public string description;
        public OtherItemEffectType effect;

        public Definition(string name, string description, OtherItemEffectType effect)
        {
            this.name = name;
            this.description = description;
            this.effect = effect;
        }
    }

    static Definition[] BuildDefinitions() => new[]
    {
        new Definition("Disassembler",
            "Drag onto a complete item and hold: it splits back into its two components.",
            OtherItemEffectType.Disassembler),
        new Definition("Delirium",
            "Drag onto a complete item and hold: it becomes a different random complete item.",
            OtherItemEffectType.Delirium),
        new Definition("Artisan's Aspect",
            "Left click: choose one complete item from a selection of five.",
            OtherItemEffectType.ArtisanAspect),
        new Definition("Nostalgia",
            "Drag onto one of your champions and hold: swaps it for a random champion of the same cost. A duplicate becomes level-up progress instead.",
            OtherItemEffectType.Nostalgia)
    };

    [MenuItem("Autobattler/Items/Generate Other Items")]
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
            string cleanName = definition.name.Replace("'", "");
            string path = $"{OutputFolder}/{cleanName}.asset";
            OtherItemData asset = AssetDatabase.LoadAssetAtPath<OtherItemData>(path);
            bool isNew = asset == null;

            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<OtherItemData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.itemName = definition.name;
            asset.description = definition.description;
            asset.effectType = definition.effect;
            if (asset.icon == null)
                asset.icon = EnsureIcon(cleanName, (float)i / definitions.Length);
            EditorUtility.SetDirty(asset);

            if (isNew) created++;
            else updated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Other items generated at {OutputFolder}: {created} created, {updated} updated.");
    }

    // Hue-tinted hexagon placeholder, distinct from equipment gems and global diamonds
    static Sprite EnsureIcon(string cleanName, float hue)
    {
        string iconFolder = $"{OutputFolder}/Icons";
        Directory.CreateDirectory(Path.Combine(Application.dataPath, iconFolder.Substring("Assets/".Length)));

        string assetPath = $"{iconFolder}/{cleanName} Icon.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (existing != null) return existing;

        Color color = Color.HSVToRGB(hue, 0.55f, 0.95f);
        Texture2D tex = HexTexture(48, color);
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

    static Texture2D HexTexture(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        float radius = half - 5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px = Mathf.Abs(x + 0.5f - half);
            float py = Mathf.Abs(y + 0.5f - half);
            // regular hexagon distance test
            bool inside = px < radius * 0.866f && py < radius && (py + px / 1.732f) < radius;
            if (!inside)
            {
                tex.SetPixel(x, y, Color.clear);
                continue;
            }

            bool rim = px > radius * 0.866f - 3f || py > radius - 3f || (py + px / 1.732f) > radius - 3f;
            float shade = 1f + 0.26f * ((y - half) / size);
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
