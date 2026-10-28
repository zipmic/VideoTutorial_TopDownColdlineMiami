using System.Collections.Generic;
using UnityEngine;

// Et våben er en "datafil" - ikke noget man sætter på et GameObject.
// Lav et nyt: Højreklik i Project-vinduet -> Create -> Top Down -> Weapon.
// Både spilleren (WeaponHolder) og fjender (EnemyAI) bruger samme filer.
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Top Down/Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Generelt")]
    public string weaponName = "Pistol";
    public Sprite weaponSprite;         // Vises i hånden og i UI
    public bool isMelee = false;        // Nærkamp i stedet for skud

    [Header("Skud")]
    public bool automatic = false;      // Hold knappen nede for at skyde
    public float shootInterval = 0.3f;  // Tid mellem hvert skud (også pause mellem nærkamps-slag)
    public int damage = 1;
    public int bulletsPerShot = 1;      // Haglgevær: fx 6
    public float spread = 3f;           // Tilfældig spredning i grader (0 = helt præcis)
    public float bulletSpeed = 15f;
    public float range = 10f;           // Kuglen forsvinder efter så mange meter
    public GameObject bulletPrefab;

    [Header("Ammo")]
    public int magazineSize = 12;       // Skud i et magasin
    public int startReserveAmmo = 36;   // Ekstra skud man starter med
    public int maxReserveAmmo = 120;    // Mere end det kan man ikke bære
    public float reloadTime = 1.2f;

    [Header("Nærkamp")]
    public float meleeRange = 1.2f;
    [Range(10f, 360f)] public float meleeArc = 100f; // Hvor bredt man rammer (grader)
    public float swingTime = 0.15f;     // Hvor lang tid sving-animationen tager

    [Header("Støj og lyd")]
    public float noiseRadius = 10f;     // Fjender inden for denne afstand hører det (nærkamp: brug fx 3)
    public AudioClip attackSound;

    // Skyder kuglerne. position = hvor de starter, aimAngle = retning i grader (0 = højre).
    // owner er den der skyder - kuglerne rammer ikke den, eller andre med samme tag.
    // targetDistance bruges af granater: de eksploderer der hvor der blev sigtet (-1 = ved max rækkevidde).
    public void Shoot(Vector2 position, float aimAngle, GameObject owner, float extraSpread = 0f, float targetDistance = -1f)
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("Våbnet '" + weaponName + "' mangler en Bullet Prefab", this);
            return;
        }

        for (int i = 0; i < bulletsPerShot; i++)
        {
            float maxSpread = spread + extraSpread;
            float angle = aimAngle + Random.Range(-maxSpread, maxSpread);
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            GameObject bullet = Instantiate(bulletPrefab, position, rotation);

            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.ownerTag = owner.tag;
                bulletScript.owner = owner.transform;
                bulletScript.damage = damage;
                bulletScript.lifeTime = range / Mathf.Max(bulletSpeed, 0.01f);
            }

            // Granater: eksploderer ved målet, og bruger våbnets skade
            Grenade grenade = bullet.GetComponent<Grenade>();
            if (grenade != null)
            {
                float flightDistance = targetDistance > 0f ? Mathf.Min(targetDistance, range) : range;
                grenade.owner = owner;
                grenade.fuseTime = flightDistance / Mathf.Max(bulletSpeed, 0.01f);

                Explosion explosion = bullet.GetComponent<Explosion>();
                if (explosion != null) explosion.damage = damage;
            }

            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.gravityScale = 0f;
                bulletRb.linearVelocity = rotation * Vector3.right * bulletSpeed;
            }
        }

        PlayAttackEffects(position, owner);
    }

    // Slår alt foran sig inden for meleeRange og meleeArc. Returnerer true hvis noget blev ramt.
    public bool MeleeAttack(Vector2 position, float aimAngle, GameObject owner)
    {
        Vector2 direction = new Vector2(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad));
        HashSet<Health> alreadyHit = new HashSet<Health>();
        bool hitSomething = false;

        foreach (Collider2D col in Physics2D.OverlapCircleAll(position, meleeRange))
        {
            Health health = col.GetComponentInParent<Health>();
            if (health == null || health.IsDead || alreadyHit.Contains(health)) continue;

            // Ikke os selv, og ikke andre på samme "hold" (fjender slår ikke hinanden)
            if (health.gameObject == owner || health.CompareTag(owner.tag)) continue;

            // Er det foran os?
            Vector2 toTarget = col.ClosestPoint(position) - position;
            if (toTarget.sqrMagnitude > 0.0001f && Vector2.Angle(direction, toTarget) > meleeArc * 0.5f) continue;

            alreadyHit.Add(health);
            health.TakeDamage(damage, position);
            hitSomething = true;
        }

        PlayAttackEffects(position, owner);
        return hitSomething;
    }

    void PlayAttackEffects(Vector2 position, GameObject owner)
    {
        if (attackSound != null)
        {
            AudioSource.PlayClipAtPoint(attackSound, position);
        }

        Noise.Make(position, noiseRadius, owner);
    }
}
