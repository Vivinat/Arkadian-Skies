using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// All the roulette "juice": staggered slot pop-in on spin, pixel spark bursts on
// acquire and level up, floating level-up text and a gold chip pop. Everything is
// spawned in code on a top-most fx layer - no particle assets involved.
public class RouletteJuiceUI : MonoBehaviour
{
    public RouletteManager roulette;
    public PlayerGold playerGold;
    public Transform slotsParent;
    public RectTransform goldChip;
    public TMP_Text floatingTextTemplate;
    public Color[] sparkColors; // optional theme override, falls back to gold tones

    static readonly Color[] DefaultSparkColors =
    {
        new Color(0.91f, 0.78f, 0.38f),
        new Color(0.97f, 0.88f, 0.55f),
        new Color(0.95f, 0.95f, 0.9f),
        new Color(0.9f, 0.55f, 0.25f)
    };

    Color[] Palette => sparkColors != null && sparkColors.Length > 0 ? sparkColors : DefaultSparkColors;

    class Spark
    {
        public RectTransform rt;
        public Image img;
        public Vector2 vel;
        public float life;
        public float maxLife;
    }

    RectTransform fxLayer;
    Coroutine chipPopRoutine;
    bool pendingLevelUp;
    int pendingLevel;

    void Awake()
    {
        Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
        GameObject layer = new GameObject("FxLayer", typeof(RectTransform));
        layer.layer = gameObject.layer;
        fxLayer = (RectTransform)layer.transform;
        fxLayer.SetParent(canvas.transform, false);
        fxLayer.anchorMin = Vector2.zero;
        fxLayer.anchorMax = Vector2.one;
        fxLayer.offsetMin = Vector2.zero;
        fxLayer.offsetMax = Vector2.zero;
    }

    void OnEnable()
    {
        roulette.OnRouletteSpun += HandleSpun;
        roulette.OnSlotAcquired += HandleAcquired;
        roulette.OnChampionLeveledUp += HandleLeveledUp;
        playerGold.OnGoldChanged += HandleGoldChanged;
    }

    void OnDisable()
    {
        roulette.OnRouletteSpun -= HandleSpun;
        roulette.OnSlotAcquired -= HandleAcquired;
        roulette.OnChampionLeveledUp -= HandleLeveledUp;
        playerGold.OnGoldChanged -= HandleGoldChanged;
    }

    void HandleSpun(List<RouletteResult> results)
    {
        if (slotsParent.gameObject.activeInHierarchy) StartCoroutine(SlotsPopRoutine());
    }

    // Fired right before OnSlotAcquired for the same purchase, so the burst can upgrade
    void HandleLeveledUp(CharacterData champion, int newLevel)
    {
        pendingLevelUp = true;
        pendingLevel = newLevel;
    }

    void HandleAcquired(int slotIndex, RouletteResult result)
    {
        Transform slot = slotIndex < slotsParent.childCount ? slotsParent.GetChild(slotIndex) : null;
        if (slot == null)
        {
            pendingLevelUp = false;
            return;
        }

        if (pendingLevelUp)
        {
            pendingLevelUp = false;
            StartCoroutine(BurstRoutine(slot.position, 28, true));
            if (floatingTextTemplate != null)
                StartCoroutine(FloatingTextRoutine(slot.position, $"LEVEL UP! Lv {pendingLevel}"));
        }
        else
        {
            StartCoroutine(BurstRoutine(slot.position, 14, false));
        }
    }

    void HandleGoldChanged(int amount)
    {
        if (goldChip == null || !gameObject.activeInHierarchy) return;
        if (chipPopRoutine != null)
        {
            StopCoroutine(chipPopRoutine);
            goldChip.localScale = Vector3.one;
        }
        chipPopRoutine = StartCoroutine(ChipPopRoutine());
    }

    IEnumerator SlotsPopRoutine()
    {
        List<Transform> slots = new List<Transform>();
        foreach (Transform child in slotsParent)
        {
            child.localScale = Vector3.zero;
            slots.Add(child);
        }

        foreach (Transform slot in slots)
        {
            StartCoroutine(PopInRoutine(slot, 0.24f));
            yield return new WaitForSeconds(0.06f);
        }
    }

    IEnumerator PopInRoutine(Transform target, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float s = 1f - Mathf.Pow(1f - p, 3f);
            s *= 1f + 0.18f * Mathf.Sin(p * Mathf.PI);
            if (target == null) yield break;
            target.localScale = Vector3.one * s;
            yield return null;
        }
        if (target != null) target.localScale = Vector3.one;
    }

    IEnumerator ChipPopRoutine()
    {
        float t = 0f;
        const float duration = 0.22f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            goldChip.localScale = Vector3.one * (1f + 0.16f * Mathf.Sin(p * Mathf.PI));
            yield return null;
        }
        goldChip.localScale = Vector3.one;
        chipPopRoutine = null;
    }

    IEnumerator BurstRoutine(Vector3 worldPos, int count, bool big)
    {
        Spark[] sparks = new Spark[count];
        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Spark", typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(fxLayer, false);
            float size = Random.Range(5f, big ? 13f : 9f);
            rt.sizeDelta = new Vector2(size, size);
            rt.position = worldPos;

            Image img = go.GetComponent<Image>();
            Color[] palette = Palette;
            img.color = palette[Random.Range(0, palette.Length)];
            img.raycastTarget = false;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(140f, big ? 460f : 320f);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.9f + 0.4f);
            sparks[i] = new Spark { rt = rt, img = img, vel = dir * speed, maxLife = Random.Range(0.45f, big ? 0.9f : 0.7f) };
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
                spark.vel += Vector2.down * 750f * dt;
                spark.rt.anchoredPosition += spark.vel * dt;
                Color c = spark.img.color;
                c.a = 1f - spark.life / spark.maxLife;
                spark.img.color = c;
            }
            yield return null;
        }
    }

    IEnumerator FloatingTextRoutine(Vector3 worldPos, string text)
    {
        TMP_Text label = Instantiate(floatingTextTemplate, fxLayer);
        label.gameObject.SetActive(true);
        label.text = text;
        label.transform.position = worldPos;

        RectTransform rt = (RectTransform)label.transform;
        Vector2 basePos = rt.anchoredPosition + new Vector2(0f, 95f);
        Color c = label.color;

        float t = 0f;
        const float duration = 1.15f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            rt.anchoredPosition = basePos + new Vector2(0f, 65f * p);
            c.a = 1f - Mathf.Clamp01((p - 0.55f) / 0.45f);
            label.color = c;
            yield return null;
        }
        Destroy(label.gameObject);
    }
}
