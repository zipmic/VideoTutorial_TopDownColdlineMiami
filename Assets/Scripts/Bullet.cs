using UnityEngine;

// Lægges på Bullet-prefab.
// Kræver: Tag "Bullet", Rigidbody2D og en Collider2D med "Is Trigger" slået til.
// Samme prefab kan bruges af både spiller og fjender.
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    public float lifeTime = 3f; // Kuglen fjernes efter denne tid
    public LayerMask hitLayers = ~0; // Hvad kuglen kan ramme (standard: alt)

    // Sættes af den gun der skyder kuglen
    [HideInInspector] public int damage = 1;
    [HideInInspector] public string ownerTag = "Player"; // Kuglen rammer ikke den der skød
    [HideInInspector] public Transform owner;             // ...eller noget der sidder på den der skød

    private Rigidbody2D rb;
    private bool hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        // Hurtige kugler kan "hoppe" over tynde vægge mellem to frames.
        // Derfor kigger vi frem: rammer vi noget på vejen i denne frame?
        Vector2 velocity = rb.linearVelocity;
        float distance = velocity.magnitude * Time.fixedDeltaTime;
        if (distance <= 0f) return;

        RaycastHit2D[] hits = Physics2D.RaycastAll(rb.position, velocity.normalized, distance, hitLayers);

        // Hits er sorteret - det tætteste først
        foreach (RaycastHit2D hit in hits)
        {
            if (HandleHit(hit.collider)) return;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    // Returnerer true hvis kuglen ramte noget og blev fjernet
    bool HandleHit(Collider2D other)
    {
        if (hasHit) return true;

        // Ignorér kuglen selv
        if (other.transform.IsChildOf(transform)) return false;

        // Ignorér den der skød (også dens gun), og andre kugler
        if (owner != null && other.transform.IsChildOf(owner)) return false;
        if (other.CompareTag(ownerTag) || other.CompareTag("Bullet")) return false;

        // Ignorér lag vi ikke skal ramme
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return false;

        hasHit = true;
        Debug.Log("Bullet ramte: " + other.name);

        // Ramte vi spilleren?
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        // Ramte vi en fjende?
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        Destroy(gameObject);
        return true;
    }
}
