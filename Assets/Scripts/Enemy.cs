using System.Collections;
using UnityEngine;

// Lægges på Enemy.
// Kræver: Tag "Enemy", Rigidbody2D og en Collider2D.
// Fjenden "ser" spilleren når han er tæt nok på (og ikke gemmer sig bag en væg).
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [Header("Liv")]
    public int maxHealth = 3;
    public Color hitColor = Color.red;  // Farven fjenden blinker når den bliver ramt
    public float hitFlashTime = 0.1f;

    [Header("Syn")]
    public float detectionRange = 6f;   // Hvor langt fjenden kan se
    public float loseSightRange = 8f;   // Når den har set dig, mister den dig først her
    public LayerMask obstacleLayers;    // Fx "Walls" - fjenden kan ikke se igennem dem

    [Header("Følg efter spilleren (0 = står stille)")]
    public float chaseSpeed = 0f;
    public float stopDistance = 3f;     // Fjenden stopper når den er så tæt på

    // Andre scripts (fx EnemyGun) kan læse disse
    [HideInInspector] public Transform player;
    [HideInInspector] public bool canSeePlayer = false;

    private int currentHealth;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color startColor;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            startColor = spriteRenderer.color;
        }
    }

    void Start()
    {
        currentHealth = maxHealth;

        // Find spilleren via tag
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    void Update()
    {
        canSeePlayer = CheckCanSeePlayer();
    }

    void FixedUpdate()
    {
        if (chaseSpeed <= 0f || !canSeePlayer)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = player.position - transform.position;

        if (toPlayer.magnitude > stopDistance)
        {
            rb.linearVelocity = toPlayer.normalized * chaseSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    bool CheckCanSeePlayer()
    {
        // Spilleren findes ikke eller er død (deaktiveret)
        if (player == null || !player.gameObject.activeInHierarchy) return false;

        Vector2 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        // Har vi allerede set spilleren, bruger vi den længere afstand
        float range = canSeePlayer ? loseSightRange : detectionRange;
        if (distance > range) return false;

        // Er der en væg imellem?
        RaycastHit2D hit = Physics2D.Raycast(transform.position, toPlayer.normalized, distance, obstacleLayers);
        if (hit.collider != null) return false;

        return true;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StopAllCoroutines();
        StartCoroutine(HitFlash());
    }

    IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(hitFlashTime);
        spriteRenderer.color = startColor;
    }

    void Die()
    {
        Destroy(gameObject);
    }

    // Tegner cirkler i Scene-vinduet, så man kan se hvor langt fjenden kan se
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseSightRange);

        if (chaseSpeed > 0f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, stopDistance);
        }
    }
}
