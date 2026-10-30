using System.Collections;
using UnityEngine;

// Lægges på fjendens Gun (som child af Enemy).
// Gun drejer rundt om fjenden mod spilleren og skyder når den sigter rigtigt.
public class EnemyGun : MonoBehaviour
{
    [Header("Placering")]
    public Enemy enemy;                 // Hvis tom, findes den på parent
    public float distanceFromEnemy = 0.7f;

    [Header("Sigte")]
    public float turnSpeed = 270f;      // Grader i sekundet gun kan dreje
    public float aimTolerance = 10f;    // Hvor præcist den skal sigte før den skyder (grader)
    public float reactionTime = 0.4f;   // Ventetid fra den ser dig til første skud

    [Header("Bullet")]
    public GameObject bulletPrefab;
    public Transform spawnPoint;
    public float bulletSpeed = 10f;
    public int damage = 1;
    public float spread = 5f;           // Tilfældig spredning i grader (0 = helt præcis)

    [Header("Lyd (må gerne være tom)")]
    public AudioClip shootSound;
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;

    [Header("Skud")]
    public float shootInterval = 0.4f;
    public int magazineSize = 6;
    public float reloadTime = 1.5f;     // Fjenden har uendeligt ammo, men skal lade om

    private float currentAngle = 0f;
    private float shootTimer = 0f;
    private float reactionTimer = 0f;
    private int currentAmmo;
    private bool isReloading = false;
    private Vector3 startScale;

    void Start()
    {
        if (enemy == null)
        {
            enemy = GetComponentInParent<Enemy>();
        }

        startScale = transform.localScale;
        currentAmmo = magazineSize;
    }

    void Update()
    {
        if (enemy == null) return;

        if (shootTimer > 0f)
        {
            shootTimer -= Time.deltaTime;
        }

        if (enemy.canSeePlayer)
        {
            // Drej langsomt mod spilleren
            Vector2 toPlayer = enemy.player.position - enemy.transform.position;
            float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.deltaTime);

            reactionTimer += Time.deltaTime;

            // Skyd hvis vi sigter nogenlunde rigtigt og har reageret
            bool isAimed = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) <= aimTolerance;
            if (isAimed && reactionTimer >= reactionTime)
            {
                TryShoot();
            }
        }
        else
        {
            // Mister den dig, skal den reagere igen næste gang
            reactionTimer = 0f;
        }

        UpdateGunTransform();
    }

    void UpdateGunTransform()
    {
        Vector2 direction = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad));

        // Placér gun rundt om fjenden
        transform.position = enemy.transform.position + (Vector3)(direction * distanceFromEnemy);
        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);

        // Vend gun på venstre side, så den ikke står på hovedet
        Vector3 scale = startScale;
        scale.y = direction.x < 0 ? -Mathf.Abs(startScale.y) : Mathf.Abs(startScale.y);
        transform.localScale = scale;
    }

    void TryShoot()
    {
        if (isReloading || shootTimer > 0f) return;

        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        Shoot();
    }

    void Shoot()
    {
        if (bulletPrefab == null) return;

        Transform firePoint = spawnPoint != null ? spawnPoint : transform;

        // Tilfældig spredning så fjenden ikke rammer perfekt hver gang
        float randomAngle = Random.Range(-spread, spread);
        Quaternion rotation = firePoint.rotation * Quaternion.Euler(0f, 0f, randomAngle);

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, rotation);

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.ownerTag = enemy.tag;
            bulletScript.owner = enemy.transform;
            bulletScript.damage = damage;
        }

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.gravityScale = 0f;
            bulletRb.linearVelocity = rotation * Vector3.right * bulletSpeed;
        }

        SoundPlayer.Play(shootSound, firePoint.position, minPitch, maxPitch);

        currentAmmo--;
        shootTimer = shootInterval;
    }

    IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = magazineSize;
        isReloading = false;
    }
}
