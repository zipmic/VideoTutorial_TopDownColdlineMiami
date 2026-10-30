using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Lægges på Gun.
// Hold venstre musetast nede for at skyde.
public class Gun : MonoBehaviour
{
    [Header("Bullet")]
    public GameObject bulletPrefab;
    public Transform spawnPoint;       // Tomt GameObject foran gun-løbet
    public float bulletSpeed = 15f;
    public int damage = 1;

    [Header("Skud")]
    public float shootInterval = 0.15f; // Tid mellem hvert skud
    public int bulletsPerShot = 1;      // Haglgevær: fx 5
    public float spread = 0f;           // Tilfældig spredning i grader (haglgevær: fx 15)

    [Header("Ammo")]
    public int magazineSize = 12;      // Skud i et magasin
    public int totalAmmo = 60;         // Ekstra skud man har med
    public float reloadTime = 1.2f;

    [Header("Lyd (må gerne være tom)")]
    public AudioClip shootSound;
    public float minPitch = 0.9f;       // Lyden er lidt forskellig hver gang
    public float maxPitch = 1.1f;

    [Header("UI (må gerne være tom)")]
    public TextMeshProUGUI ammoText;

    private int currentAmmo;
    private float shootTimer = 0f;
    private bool isReloading = false;

    void Start()
    {
        currentAmmo = magazineSize;
        UpdateAmmoText();
    }

    // Når man skifter til dette våben
    void OnEnable()
    {
        UpdateAmmoText();
    }

    // Når man skifter væk fra dette våben: stop en halvfærdig reload
    void OnDisable()
    {
        StopAllCoroutines();
        isReloading = false;
    }

    void Update()
    {
        if (shootTimer > 0f)
        {
            shootTimer -= Time.deltaTime;
        }

        if (isReloading) return;

        // Auto reload når magasinet er tomt
        if (currentAmmo <= 0)
        {
            if (totalAmmo > 0)
            {
                StartCoroutine(Reload());
            }
            return;
        }

        // Tryk R for at reloade - kun hvis magasinet ikke er fuldt og der er ammo tilbage
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (currentAmmo < magazineSize && totalAmmo > 0)
            {
                StartCoroutine(Reload());
                return;
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed && shootTimer <= 0f)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null) return;

        // Hvis der ikke er sat et spawnpoint, skyder vi fra gun selv
        Transform firePoint = spawnPoint != null ? spawnPoint : transform;

        for (int i = 0; i < bulletsPerShot; i++)
        {
            // Tilfældig spredning (0 = helt præcis)
            float randomAngle = Random.Range(-spread, spread);
            Quaternion rotation = firePoint.rotation * Quaternion.Euler(0f, 0f, randomAngle);

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, rotation);

            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.ownerTag = "Player";
                bulletScript.owner = transform.parent != null ? transform.parent : transform; // Spilleren (gun er child af spilleren)
                bulletScript.damage = damage;
            }

            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.gravityScale = 0f;
                bulletRb.linearVelocity = rotation * Vector3.right * bulletSpeed;
            }
        }

        SoundPlayer.Play(shootSound, firePoint.position, minPitch, maxPitch);

        currentAmmo--;
        shootTimer = shootInterval;
        UpdateAmmoText();
    }

    IEnumerator Reload()
    {
        isReloading = true;

        if (ammoText != null)
        {
            ammoText.text = "Reloading...";
        }

        yield return new WaitForSeconds(reloadTime);

        // Tag så mange skud som muligt fra totalAmmo
        int needed = magazineSize - currentAmmo;
        int amount = Mathf.Min(needed, totalAmmo);

        currentAmmo += amount;
        totalAmmo -= amount;

        isReloading = false;
        UpdateAmmoText();
    }

    void UpdateAmmoText()
    {
        if (ammoText == null) return;

        ammoText.text = currentAmmo + " / " + totalAmmo;
    }

    // Kan kaldes fra fx en ammo-pickup
    public void AddAmmo(int amount)
    {
        totalAmmo += amount;
        UpdateAmmoText();
    }
}
