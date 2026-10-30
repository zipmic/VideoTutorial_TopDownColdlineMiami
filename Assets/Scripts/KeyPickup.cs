using UnityEngine;

// Lægges på en nøgle.
// Kræver: en Collider2D med "Is Trigger" slået til.
// Nøglen forsvinder når spilleren tager den. Døren (Door) holder øje med om den er væk.
public class KeyPickup : MonoBehaviour
{
    public AudioClip pickupSound;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        SoundPlayer.Play(pickupSound, transform.position);
        Destroy(gameObject);
    }
}
