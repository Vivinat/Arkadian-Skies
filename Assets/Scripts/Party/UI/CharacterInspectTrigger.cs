using UnityEngine;
using UnityEngine.EventSystems;

// Drop this on the same GameObject as any portrait button (bench slot, party slot, roulette
// slot...) to make right-clicking it open the Character Info Panel. Reads the champion from
// whatever component on this GameObject implements IChampionProvider - it never needs to know
// which one that is.
public class CharacterInspectTrigger : MonoBehaviour, IPointerClickHandler
{
    IChampionProvider provider;

    void Awake()
    {
        provider = GetComponent<IChampionProvider>();
        if (provider == null)
            Debug.LogWarning($"CharacterInspectTrigger on '{name}' found no IChampionProvider - right-click inspect will do nothing.", this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (provider == null) return;

        CharacterData champion = provider.GetInspectedChampion();
        if (champion == null) return;

        CharacterInfoPanelController.Instance?.Show(champion);
    }
}