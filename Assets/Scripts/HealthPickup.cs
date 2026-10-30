using UnityEngine;

// Lægges på en health pack / first aid.
// Kræver: en Collider2D med "Is Trigger" slået til.
public class HealthPickup : MonoBehaviour
{
    public int healAmount = 1;
    public AudioClip pickupSound;

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.Heal(healAmount);
        SoundPlayer.Play(pickupSound, transform.position);
        Destroy(gameObject);
    }
}
