using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Placeholder unit visual: shows the champion's portrait and name, dims on death,
// flashes when hit, and opens the Character Info Panel on right click.
public class BattleUnitView : MonoBehaviour, IPointerClickHandler
{
    public Image portraitImage;
    public TMP_Text nameLabel; // optional

    static readonly Color DeadTint = new Color(0.4f, 0.35f, 0.45f, 0.55f);
    static readonly Color HitTint = new Color(1f, 0.45f, 0.45f);

    BattleUnit unit;
    bool shownDead;
    Coroutine hitFlashRoutine;

    public BattleUnit Unit => unit;

    public void Bind(BattleUnit newUnit)
    {
        unit = newUnit;
        shownDead = false;
        gameObject.SetActive(unit != null);
        if (unit == null) return;

        portraitImage.sprite = unit.data.portrait;
        portraitImage.color = Color.white;
        if (nameLabel != null) nameLabel.text = unit.data.characterName;
    }

    void Update()
    {
        if (unit == null || shownDead) return;

        if (!unit.IsAlive)
        {
            shownDead = true;
            portraitImage.color = DeadTint;
        }
    }

    public void PlayHitFlash()
    {
        if (!isActiveAndEnabled || unit == null || !unit.IsAlive) return;
        if (hitFlashRoutine != null) StopCoroutine(hitFlashRoutine);
        hitFlashRoutine = StartCoroutine(HitFlashRoutine());
    }

    IEnumerator HitFlashRoutine()
    {
        portraitImage.color = HitTint;
        yield return new WaitForSeconds(0.12f);
        portraitImage.color = unit != null && unit.IsAlive ? Color.white : DeadTint;
        hitFlashRoutine = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (unit == null || !unit.IsAlive) return; // only units actually fighting can be inspected

        // The provider keeps the panel's ACTIVE ability marker live - it reads the unit's
        // current abilityPosition, which abilities like Nikkal's reposition change mid-battle
        BattleUnit inspected = unit;
        CharacterInfoPanelController.Instance?.Show(inspected.data,
            () => inspected.IsAlive ? inspected.abilityPosition : (BattlePosition?)null);
    }
}
