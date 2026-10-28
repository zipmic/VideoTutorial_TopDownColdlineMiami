using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum PickupType
{
    Health, // Giver liv
    Ammo,   // Giver ammo til det man holder (eller et bestemt våben)
    Weapon, // Giver et nyt våben
    Key,    // Giver en nøgle til en dør
    Score   // Samleobjekt der giver bonuspoint (energidrik, drugs, guld... skift selv sprite og lyd)
}

// Lægges på alle ting man kan samle op. Vælg Type i Inspector - så er det en health pack,
// ammo-pakke, våben eller nøgle. Lav en prefab pr. slags og træk dem ind i banen.
// Kræver en Collider2D (Is Trigger sættes automatisk).
[RequireComponent(typeof(Collider2D))]
public class Pickup : MonoBehaviour
{
    [Header("Hvad er det?")]
    public PickupType type = PickupType.Health;
    public int amount = 1;              // Health: liv. Ammo: skud. Weapon: ammo i reserven (0 = våbnets standard). Score: point
    public WeaponData weapon;           // Weapon: hvilket våben. Ammo: kun til dette våben (tom = det man holder)
    public string keyId = "red";        // Key: dør med samme keyId kan åbnes

    [Header("Regler")]
    public bool onlyWhenNeeded = true;  // Health pack ligger der indtil man faktisk mangler liv
    public float respawnTime = 0f;      // 0 = forsvinder for altid

    [Header("Effekter (må gerne være tomme)")]
    public float bobHeight = 0.05f;     // Svæver lidt op og ned
    public float bobSpeed = 3f;
    public AudioClip pickupSound;
    public UnityEvent onPickedUp;

    private Vector3 startPosition;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private bool isHidden = false;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    void Start()
    {
        startPosition = transform.position;

        // Samleobjekter melder sig til ScoreManager, så den kan vise "fundet x / y"
        if (type == PickupType.Score)
        {
            if (ScoreManager.Instance != null) ScoreManager.Instance.RegisterCollectible();
            else Debug.LogWarning("Score-pickup uden en ScoreManager i banen", this);
        }

        // Våben-pickups viser våbnets egen sprite
        if (type == PickupType.Weapon && weapon != null && weapon.weaponSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = weapon.weaponSprite;
        }
    }

    void Update()
    {
        if (bobHeight > 0f && !isHidden)
        {
            transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other);
    }

    // Også Stay: så man kan samle en health pack op når man senere mangler liv, uden at gå væk og tilbage
    void OnTriggerStay2D(Collider2D other)
    {
        TryCollect(other);
    }

    void TryCollect(Collider2D other)
    {
        if (isHidden) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        if (!Apply(player)) return;

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        onPickedUp.Invoke();

        if (respawnTime > 0f)
        {
            StartCoroutine(Respawn());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Returnerer true hvis spilleren faktisk fik noget
    bool Apply(PlayerController player)
    {
        switch (type)
        {
            case PickupType.Health:
                Health health = player.GetComponent<Health>();
                if (health == null || health.IsDead) return false;
                if (onlyWhenNeeded && health.IsFull) return false;
                health.Heal(amount);
                return true;

            case PickupType.Ammo:
                WeaponHolder ammoHolder = player.GetComponent<WeaponHolder>();
                return ammoHolder != null && ammoHolder.AddAmmo(amount, weapon);

            case PickupType.Weapon:
                WeaponHolder weaponHolder = player.GetComponent<WeaponHolder>();
                return weaponHolder != null && weaponHolder.PickupWeapon(weapon, amount);

            case PickupType.Key:
                PlayerInventory inventory = player.GetComponent<PlayerInventory>();
                if (inventory == null) return false;
                inventory.AddKey(keyId);
                return true;

            case PickupType.Score:
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.CollectibleFound(amount, transform.position);
                }
                return true;
        }

        return false;
    }

    IEnumerator Respawn()
    {
        SetHidden(true);
        yield return new WaitForSeconds(respawnTime);
        SetHidden(false);
    }

    void SetHidden(bool hidden)
    {
        isHidden = hidden;
        col.enabled = !hidden;
        if (spriteRenderer != null) spriteRenderer.enabled = !hidden;
    }
}
