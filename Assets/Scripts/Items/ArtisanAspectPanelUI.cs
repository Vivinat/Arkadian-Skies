using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Modal for Artisan's Aspect: shows up to 5 complete items to pick one. Each choice
// tooltips its own description; picking one fires the callback and closes.
public class ArtisanAspectPanelUI : MonoBehaviour
{
    [Serializable]
    public class Choice
    {
        public GameObject root;
        public Button button;
        public Image icon;
        public TMP_Text nameLabel;
        public ItemTooltipTrigger tooltip;
    }

    public GameObject panelRoot;
    public Button closeButton;
    public Choice[] choices;

    Action<ItemComponentData> onChosen;

    void Awake()
    {
        panelRoot.SetActive(false);
        for (int i = 0; i < choices.Length; i++)
        {
            int index = i;
            choices[i].button.onClick.AddListener(() => Pick(index));
        }
    }

    void OnEnable()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    void OnDisable()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    List<ItemComponentData> current = new List<ItemComponentData>();

    public void Open(List<ItemComponentData> items, Action<ItemComponentData> chosenCallback)
    {
        onChosen = chosenCallback;
        current = items;

        for (int i = 0; i < choices.Length; i++)
        {
            bool has = i < items.Count && items[i] != null;
            choices[i].root.SetActive(has);
            if (!has) continue;

            ItemComponentData item = items[i];
            choices[i].icon.sprite = item.icon;
            choices[i].icon.enabled = item.icon != null;
            choices[i].nameLabel.text = item.itemName;
            choices[i].tooltip.SetItem(item);
        }

        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
    }

    void Pick(int index)
    {
        ItemComponentData chosen = index < current.Count ? current[index] : null;
        onChosen?.Invoke(chosen);
        Close();
    }

    void Close()
    {
        AbilityTooltip.Instance?.Hide();
        panelRoot.SetActive(false);
    }
}
