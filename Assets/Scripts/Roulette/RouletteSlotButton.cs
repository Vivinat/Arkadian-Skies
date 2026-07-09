using UnityEngine;
using UnityEngine.UI;

// Goes on the slot prefab itself: a Button whose own Image IS the portrait (no child text).
public class RouletteSlotButton : MonoBehaviour, IChampionProvider
{
    public Image portraitImage;
    public Button button;

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
    }

    public void SetClaimed(bool claimed)
    {
        button.interactable = !claimed;
    }

    public CharacterData GetInspectedChampion() => Champion;
}