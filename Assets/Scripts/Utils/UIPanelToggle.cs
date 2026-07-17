using UnityEngine;
using UnityEngine.UI;

public class UIPanelToggle : MonoBehaviour
{
    public Button button;
    public GameObject panel;

    void OnEnable() => button.onClick.AddListener(Toggle);
    void OnDisable() => button.onClick.RemoveListener(Toggle);

    void Toggle() => panel.SetActive(!panel.activeSelf);
}
