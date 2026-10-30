using UnityEngine;

// Lægges på et våben der ligger på gulvet.
// Kræver: en Collider2D med "Is Trigger" slået til.
// Weapon Name skal være præcis det samme som navnet på våben-objektet under Player (fx "Shotgun").
public class WeaponPickup : MonoBehaviour
{
    public string weaponName = "Shotgun";
    public AudioClip pickupSound;

    void OnTriggerEnter2D(Collider2D other)
    {
        WeaponSwitcher switcher = other.GetComponentInParent<WeaponSwitcher>();
        if (switcher == null) return;

        if (switcher.GiveWeapon(weaponName))
        {
            SoundPlayer.Play(pickupSound, transform.position);
            Destroy(gameObject);
        }
    }
}
