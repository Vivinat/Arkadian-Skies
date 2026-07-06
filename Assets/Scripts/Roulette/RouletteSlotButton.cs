using UnityEngine;
using UnityEngine.UI;

// Goes on the slot prefab itself: a Button whose own Image IS the portrait (no child text).
public class RouletteSlotButton : MonoBehaviour
{
    public Image portraitImage;
    public Button button;

    void Reset()
    {
        portraitImage = GetComponent<Image>();
        button = GetComponent<Button>();
    }

    public void SetChampion(Sprite portrait)
    {
        portraitImage.sprite = portrait;
        portraitImage.enabled = portrait != null;
        button.interactable = portrait != null;
    }

    public void SetClaimed(bool claimed)
    {
        button.interactable = !claimed;
    }
}