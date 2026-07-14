using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One instance per scene, living on its own panel directly under the root Canvas (start disabled).
// Any system that needs a champion to "speak" - Roulette on acquire/level-up, Battle on the
// killing blow that wins a fight, etc. - just calls Show(). Any key or mouse button press dismisses it.
public class DialogueUIController : MonoBehaviour
{
    public static DialogueUIController Instance { get; private set; }

    [Header("Refs")]
    public GameObject panelRoot;
    public Image portraitImage;
    public TMP_Text nameLabel;
    public TMP_Text quoteLabel;

    bool isVisible;
    int shownAtFrame;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        panelRoot.SetActive(false);
    }

    void Update()
    {
        // Time.frameCount check stops the panel from closing on the very same frame it was opened,
        // in case Show() itself was triggered by the same click/key press that Input.anyKeyDown sees
        if (isVisible && Input.anyKeyDown && Time.frameCount > shownAtFrame) Hide();
    }

    public void Show(CharacterData champion, ChampionQuoteType quoteType)
    {
        if (champion == null) return;

        string quote = GetQuote(champion, quoteType);
        if (string.IsNullOrEmpty(quote)) return;

        Show(champion.portrait, champion.characterName, quote);
    }

    public void Show(Sprite portrait, string speakerName, string quoteText)
    {
        portraitImage.sprite = portrait;
        nameLabel.text = speakerName;
        quoteLabel.text = quoteText;

        isVisible = true;
        shownAtFrame = Time.frameCount;
        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
    }

    public void Hide()
    {
        isVisible = false;
        panelRoot.SetActive(false);
    }

    static string GetQuote(CharacterData champion, ChampionQuoteType quoteType)
    {
        switch (quoteType)
        {
            case ChampionQuoteType.Select: return champion.selectQuote;
            case ChampionQuoteType.LevelUp: return champion.levelUpQuote;
            case ChampionQuoteType.Victory: return champion.victoryQuote;
            default: return null;
        }
    }
}