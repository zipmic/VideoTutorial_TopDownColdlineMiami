using UnityEngine;

// Lægges på en ammo-kasse.
// Kræver: en Collider2D med "Is Trigger" slået til.
// Skriv en Ammo Type (fx "shotgun"): så får alle våben med samme Ammo Type ammo, også dem man ikke holder.
// Lad Ammo Type stå tom: så får man ammo til det våben man holder.
public class AmmoPickup : MonoBehaviour
{
    public int ammoAmount = 20;
    public string ammoType = "";
    public AudioClip pickupSound;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        bool gaveAmmo = false;

        if (string.IsNullOrEmpty(ammoType))
        {
            // Finder det våben der er tændt lige nu
            Gun gun = other.GetComponentInChildren<Gun>();
            if (gun != null)
            {
                gun.AddAmmo(ammoAmount);
                gaveAmmo = true;
            }
        }
        else
        {
            // true = find også våben der er slukket (dem man ikke holder)
            foreach (Gun gun in other.GetComponentsInChildren<Gun>(true))
            {
                if (gun.ammoType == ammoType)
                {
                    gun.AddAmmo(ammoAmount);
                    gaveAmmo = true;
                }
            }
        }

        // Passer kassen ikke til noget våben, bliver den liggende
        if (!gaveAmmo) return;

        SoundPlayer.Play(pickupSound, transform.position);
        Destroy(gameObject);
    }
}
