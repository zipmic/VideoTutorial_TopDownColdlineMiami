using UnityEngine;

// Lægges på en ammo-kasse.
// Kræver: en Collider2D med "Is Trigger" slået til.
// Giver ammo til det våben spilleren holder.
public class AmmoPickup : MonoBehaviour
{
    public int ammoAmount = 20;
    public AudioClip pickupSound;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Finder det våben der er tændt lige nu
        Gun gun = other.GetComponentInChildren<Gun>();
        if (gun == null) return;

        gun.AddAmmo(ammoAmount);
        SoundPlayer.Play(pickupSound, transform.position);
        Destroy(gameObject);
    }
}
