using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Lægges på et tomt GameObject i banen (fx "GameManager") - IKKE på Player, for Player slukkes når man dør.
// Når spilleren er død, starter banen forfra med R.
// (Er man i live, bruges R stadig til at reloade.)
public class RetryLevel : MonoBehaviour
{
    public GameObject player;      // Tom = finder objektet med tag Player
    public GameObject retryText;   // Valgfri: tekst/UI der vises når man er død, fx "Tryk R for at prøve igen"

    void Start()
    {
        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
        }

        if (retryText != null) retryText.SetActive(false);
    }

    void Update()
    {
        if (player == null || player.activeInHierarchy) return;

        if (retryText != null) retryText.SetActive(true);

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
