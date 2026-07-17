using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Goes on the slot prefab itself: a Button whose own Image IS the portrait (no child text).
public class RouletteSlotButton : MonoBehaviour, IChampionProvider
{
    public Image portraitImage;
    public Button button;
    public TMP_Text badgeLabel; // optional: ownership/level-up progress strip

    static readonly Color NewBadgeColor = new Color(0.91f, 0.78f, 0.38f);
    static readonly Color OwnedBadgeColor = new Color(0.93f, 0.91f, 0.86f);

    public CharacterData Champion { get; private set; }

    void Reset()
    {
        portraitImage = GetComponent<Image>();
        button = GetComponent<Button>();
    }

    public void SetChampion(CharacterData champion)
    {
        Champion = champion;
        portraitImage.sprite = champion != null ? champion.portrait : null;
        portraitImage.enabled = champion != null;
        button.interactable = champion != null;

        if (badgeLabel != null && champion == null) badgeLabel.text = "";
    }

    public void SetClaimed(bool claimed)
    {
        button.interactable = !claimed;
    }

    // Gold "NEW" for unowned champions; owned ones show level + copies collected towards
    // the next level up, turning gold when a single copy away
    public void SetOwnership(bool owned, int level, int mementos, int needed)
    {
        if (badgeLabel == null) return;

        if (Champion == null)
        {
            badgeLabel.text = "";
            return;
        }

        if (!owned)
        {
            badgeLabel.text = "NEW";
            badgeLabel.color = NewBadgeColor;
            return;
        }

        bool oneCopyAway = mementos == needed - 1;
        badgeLabel.text = $"Lv {level}  {mementos}/{needed}";
        badgeLabel.color = oneCopyAway ? NewBadgeColor : OwnedBadgeColor;
    }

    public CharacterData GetInspectedChampion() => Champion;
}
