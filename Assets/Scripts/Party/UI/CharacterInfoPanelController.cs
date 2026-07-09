using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One instance per scene, living on the panel's root (start disabled, direct child of the root
// Canvas, anchored at its center). Any CharacterInspectTrigger just calls Show() on it.
public class CharacterInfoPanelController : MonoBehaviour
{
    public static CharacterInfoPanelController Instance { get; private set; }

    [Header("Refs")]
    public PlayerRoster roster; // optional - if null, level always shows as 1 (e.g. inspecting an enemy/boss)
    public GameObject panelRoot;
    public Button closeButton;

    [Header("Identity")]
    public Image portraitImage;
    public TMP_Text nameLabel;
    public TMP_Text levelLabel;

    [Header("Attributes")]
    public TMP_Text hpLabel;
    public TMP_Text adLabel;
    public TMP_Text apLabel;
    public TMP_Text atkSpeedLabel;
    public TMP_Text critLabel;
    public TMP_Text defLabel;
    public TMP_Text mdefLabel;
    public TMP_Text resourceLabel;

    [Header("Abilities")]
    public AbilityRowUI frontlineAbilityRow;
    public AbilityRowUI backlineAbilityRow;
    

    CharacterData currentChampion;
    RectTransform panelRect;
    RectTransform canvasRect;
    Camera uiCamera;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        panelRect = (RectTransform)panelRoot.transform;
        Canvas canvas = panelRoot.GetComponentInParent<Canvas>().rootCanvas;
        canvasRect = (RectTransform)canvas.transform;
        uiCamera = UIPositioning.CameraFor(canvas);

        panelRoot.SetActive(false);
    }

    void OnEnable()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    void OnDisable()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
    }


    public void Show(CharacterData champion)
    {
        if (champion == null) return;

        currentChampion = champion;
        PopulateContent(champion);

        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
    }


    public void Hide()
    {
        currentChampion = null;
        panelRoot.SetActive(false);
        AbilityTooltip.Instance?.Hide();
    }

    void PopulateContent(CharacterData champion)
    {
        int level = roster != null ? Mathf.Max(roster.GetLevel(champion), 1) : 1;

        portraitImage.sprite = champion.portrait;
        nameLabel.text = champion.characterName;
        levelLabel.text = $"Lv. {level}";

        hpLabel.text = $"HP: {champion.maxHP:0}";
        adLabel.text = $"AD: {champion.AD:0}";
        apLabel.text = $"AP: {champion.AP:0}";
        atkSpeedLabel.text = $"ATK SPD: {champion.attackSpeed:0.00}";
        critLabel.text = $"CRIT: {champion.critChance * 100f:0}%";
        defLabel.text = $"DEF: {champion.DEF:0}";
        mdefLabel.text = $"MDEF: {champion.MDEF:0}";
        resourceLabel.text = champion.manaPerSecond > 0f ? $"Mana/s: {champion.manaPerSecond:0.0}" : "Resource: none";

        frontlineAbilityRow.Bind(champion.frontlineAbility, "F");
        backlineAbilityRow.Bind(champion.backlineAbility, "B");
    }
}
