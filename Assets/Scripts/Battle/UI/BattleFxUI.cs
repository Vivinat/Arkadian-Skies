using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Battle juice: ability-name popups over the caster, hit flashes and pixel spark
// bursts on hits/kills, and a golden rain on victory. Everything spawns in code on a
// top-most fx layer.
public class BattleFxUI : MonoBehaviour
{
    public BattleSimulator simulator;
    public BattleUIManager battleUI;
    public RectTransform abilityPopupTemplate;
    public Sprite castRingSprite;

    static readonly Color[] HitSparkColors =
    {
        new Color(0.95f, 0.95f, 0.9f),
        new Color(0.94f, 0.45f, 0.45f),
        new Color(0.9f, 0.7f, 0.4f)
    };

    static readonly Color[] KillSparkColors =
    {
        new Color(0.66f, 0.55f, 0.85f),
        new Color(0.35f, 0.85f, 0.91f),
        new Color(0.95f, 0.95f, 0.9f)
    };

    static readonly Color[] VictorySparkColors =
    {
        new Color(0.97f, 0.87f, 0.47f),
        new Color(1f, 0.95f, 0.75f),
        new Color(0.35f, 0.85f, 0.91f)
    };

    class Spark
    {
        public RectTransform rt;
        public Image img;
        public Vector2 vel;
        public float life;
        public float maxLife;
    }

    RectTransform fxLayer;

    void Awake()
    {
        Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
        GameObject layer = new GameObject("BattleFxLayer", typeof(RectTransform));
        layer.layer = gameObject.layer;
        fxLayer = (RectTransform)layer.transform;
        fxLayer.SetParent(canvas.transform, false);
        fxLayer.anchorMin = Vector2.zero;
        fxLayer.anchorMax = Vector2.one;
        fxLayer.offsetMin = Vector2.zero;
        fxLayer.offsetMax = Vector2.zero;
    }

    void Start()
    {
        simulator.Events.OnAbilityUsed += HandleAbilityUsed;
        simulator.Events.OnAutoAttack += HandleAutoAttack;
        simulator.Events.OnKill += HandleKill;
        simulator.OnBattleEnded += HandleBattleEnded;
    }

    void OnDestroy()
    {
        if (simulator == null) return;
        if (simulator.Events != null)
        {
            simulator.Events.OnAbilityUsed -= HandleAbilityUsed;
            simulator.Events.OnAutoAttack -= HandleAutoAttack;
            simulator.Events.OnKill -= HandleKill;
        }
        simulator.OnBattleEnded -= HandleBattleEnded;
    }

    // The ability comes from the event itself - reading caster.GetAbility() here would be
    // wrong for casts that change the caster's kit mid-execute (Nikkal's reposition)
    void HandleAbilityUsed(BattleUnit caster, Ability ability, string displayLabel)
    {
        BattleUnitView view = battleUI.FindView(caster);
        if (view == null) return;

        string text = !string.IsNullOrEmpty(displayLabel) ? displayLabel : ability != null ? ability.abilityName : "";
        if (!string.IsNullOrEmpty(text))
            StartCoroutine(AbilityPopupRoutine(view.transform.position, text));

        StartCoroutine(CastRingRoutine(view.transform.position));
    }

    void HandleAutoAttack(BattleUnit attacker, BattleUnit target)
    {
        BattleUnitView view = battleUI.FindView(target);
        if (view == null) return;

        view.PlayHitFlash();
        StartCoroutine(BurstRoutine(view.transform.position, 4, HitSparkColors, 240f));
    }

    void HandleKill(BattleUnit killer, BattleUnit victim)
    {
        BattleUnitView view = battleUI.FindView(victim);
        if (view == null) return;
        StartCoroutine(BurstRoutine(view.transform.position, 20, KillSparkColors, 420f));
    }

    void HandleBattleEnded(bool allyWon)
    {
        if (allyWon) StartCoroutine(VictoryRainRoutine());
    }

    IEnumerator AbilityPopupRoutine(Vector3 worldPos, string abilityName)
    {
        RectTransform popup = Instantiate(abilityPopupTemplate, fxLayer);
        popup.gameObject.SetActive(true);
        popup.position = worldPos;

        TMP_Text label = popup.GetComponentInChildren<TMP_Text>();
        label.text = abilityName;

        Vector2 basePos = popup.anchoredPosition + new Vector2(0f, 108f);
        popup.anchoredPosition = basePos;

        CanvasGroup group = popup.gameObject.AddComponent<CanvasGroup>();
        float t = 0f;
        const float duration = 1.2f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            popup.anchoredPosition = basePos + new Vector2(0f, 34f * p);
            popup.localScale = Vector3.one * (0.85f + 0.15f * Mathf.Min(p * 6f, 1f));
            group.alpha = 1f - Mathf.Clamp01((p - 0.62f) / 0.38f);
            yield return null;
        }
        Destroy(popup.gameObject);
    }

    IEnumerator CastRingRoutine(Vector3 worldPos)
    {
        GameObject go = new GameObject("CastRing", typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(fxLayer, false);
        rt.sizeDelta = new Vector2(150, 186);
        rt.position = worldPos;

        Image img = go.GetComponent<Image>();
        img.sprite = castRingSprite;
        img.type = castRingSprite != null && castRingSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        img.color = new Color(0.35f, 0.85f, 0.91f, 0.9f);
        img.raycastTarget = false;

        float t = 0f;
        const float duration = 0.4f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            rt.localScale = Vector3.one * (1f + 0.25f * p);
            Color c = img.color;
            c.a = 0.9f * (1f - p);
            img.color = c;
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator VictoryRainRoutine()
    {
        for (int wave = 0; wave < 10; wave++)
        {
            Vector3 pos = fxLayer.TransformPoint(new Vector3(Random.Range(-700f, 700f), Random.Range(-80f, 320f), 0f));
            StartCoroutine(BurstRoutine(pos, 12, VictorySparkColors, 360f));
            yield return new WaitForSeconds(0.22f);
        }
    }

    IEnumerator BurstRoutine(Vector3 worldPos, int count, Color[] palette, float maxSpeed)
    {
        Spark[] sparks = new Spark[count];
        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Spark", typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(fxLayer, false);
            float size = Random.Range(4f, 10f);
            rt.sizeDelta = new Vector2(size, size);
            rt.position = worldPos;

            Image img = go.GetComponent<Image>();
            img.color = palette[Random.Range(0, palette.Length)];
            img.raycastTarget = false;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(90f, maxSpeed);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.9f + 0.35f);
            sparks[i] = new Spark { rt = rt, img = img, vel = dir * speed, maxLife = Random.Range(0.35f, 0.7f) };
        }

        bool alive = true;
        while (alive)
        {
            alive = false;
            float dt = Time.deltaTime;
            foreach (Spark spark in sparks)
            {
                if (spark.rt == null) continue;
                spark.life += dt;
                if (spark.life >= spark.maxLife)
                {
                    Destroy(spark.rt.gameObject);
                    continue;
                }

                alive = true;
                spark.vel += Vector2.down * 720f * dt;
                spark.rt.anchoredPosition += spark.vel * dt;
                Color c = spark.img.color;
                c.a = 1f - spark.life / spark.maxLife;
                spark.img.color = c;
            }
            yield return null;
        }
    }
}
