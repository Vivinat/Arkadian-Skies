using UnityEngine;

// Attach directly to MetaGameManager. Keeps it (and everything on it - PlayerRoster,
// PlayerParty, PlayerGold, RouletteManager, etc.) alive across scene loads, and guards
// against a duplicate showing up if a scene containing another MetaGameManager loads again.
public class PersistentSingleton : MonoBehaviour
{
    static PersistentSingleton instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}