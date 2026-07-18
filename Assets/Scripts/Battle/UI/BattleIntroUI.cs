using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pre-battle intro: announces each enemy by name in the top bar while a blinking frame
// focuses its sprite, then flashes "BATTLE START!" and releases the simulator.
public class BattleIntroUI : MonoBehaviour
{
    public BattleSimulator simulator;
    public BattleUIManager battleUI;
    public TMP_Text announceLabel;
    public RectTransform focusFrame;
    public float perEnemySeconds = 1.1f;

    void Start() => StartCoroutine(IntroRoutine());

    IEnumerator IntroRoutine()
    {
        announceLabel.text = "";
        yield return new WaitForSeconds(0.6f);

        List<BattleUnitView> enemies = new List<BattleUnitView>();
        Collect(battleUI.enemyFrontlineSlots, enemies);
        Collect(battleUI.enemyBacklineSlots, enemies);

        Image frameImage = focusFrame.GetComponent<Image>();
        foreach (BattleUnitView view in enemies)
        {
            announceLabel.text = view.Unit.data.characterName;
            focusFrame.gameObject.SetActive(true);
            focusFrame.position = view.transform.position;

            float t = 0f;
            while (t < perEnemySeconds)
            {
                t += Time.deltaTime;
                Color c = frameImage.color;
                c.a = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 9f));
                frameImage.color = c;
                yield return null;
            }
        }
        focusFrame.gameObject.SetActive(false);

        announceLabel.text = "BATTLE START!";
        yield return new WaitForSeconds(0.9f);
        announceLabel.text = "";

        simulator.BeginBattle();
    }

    static void Collect(CharacterSlotUI[] slots, List<BattleUnitView> into)
    {
        if (slots == null) return;
        foreach (CharacterSlotUI slot in slots)
            if (slot.unitView != null && slot.unitView.Unit != null) into.Add(slot.unitView);
    }
}
