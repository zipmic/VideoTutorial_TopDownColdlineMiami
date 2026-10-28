using UnityEngine;

// Lægges på en Grenade-prefab (sprite + Rigidbody2D + Explosion). Brug prefaben som "Bullet Prefab"
// i et WeaponData. Granaten flyver derhen hvor der blev sigtet, og eksploderer der (eller når den rammer noget).
[RequireComponent(typeof(Rigidbody2D), typeof(Explosion))]
public class Grenade : MonoBehaviour
{
    public bool explodeOnContact = true;    // Eksploderer når den rammer en væg eller en fjende
    public float spinSpeed = 360f;

    // Sættes af WeaponData når granaten kastes
    [HideInInspector] public float fuseTime = 1f;
    [HideInInspector] public GameObject owner;

    private Explosion explosion;
    private float timer = 0f;

    void Awake()
    {
        explosion = GetComponent<Explosion>();
    }

    void Update()
    {
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer >= fuseTime)
        {
            Detonate();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HitSomething(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HitSomething(collision.collider);
    }

    void HitSomething(Collider2D other)
    {
        if (!explodeOnContact || other.isTrigger) return;

        // Ikke den der kastede den, og ikke dens venner
        if (owner != null && (other.transform.IsChildOf(owner.transform) || other.CompareTag(owner.tag))) return;

        Detonate();
    }

    void Detonate()
    {
        explosion.Explode(owner);
        Destroy(gameObject);
    }
}
