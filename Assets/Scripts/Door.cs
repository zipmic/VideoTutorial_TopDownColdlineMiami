using UnityEngine;

// Lægges på en dør.
// Kræver: en Collider2D der IKKE er trigger (så man ikke kan gå igennem).
// Træk nøglen fra Hierarchy ind i "Key". Når spilleren har taget nøglen og går ind i døren, åbner den.
// Er "Key" tom, åbner døren bare når spilleren går ind i den.
public class Door : MonoBehaviour
{
    public GameObject key;

    [Header("Lyd (må gerne være tom)")]
    public AudioClip openSound;
    public AudioClip lockedSound;

    private bool needsKey;

    void Start()
    {
        needsKey = key != null;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        // Nøglen er "null" når den er samlet op (destroyed)
        bool hasKey = !needsKey || key == null;

        if (hasKey)
        {
            SoundPlayer.Play(openSound, transform.position);
            gameObject.SetActive(false); // Døren forsvinder
        }
        else
        {
            SoundPlayer.Play(lockedSound, transform.position);
            Debug.Log("Døren er låst - find nøglen!");
        }
    }
}
