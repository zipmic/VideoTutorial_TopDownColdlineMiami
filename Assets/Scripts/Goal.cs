using UnityEngine;
using UnityEngine.SceneManagement;

// Lægges på målet i banen.
// Kræver: en Collider2D med "Is Trigger" slået til.
// Når spilleren rører målet, skiftes der til banen med navnet i "Next Scene".
// Banen skal være tilføjet i File > Build Profiles > Scene List (eller Build Settings).
public class Goal : MonoBehaviour
{
    public string nextScene;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        SceneManager.LoadScene(nextScene);
    }
}
