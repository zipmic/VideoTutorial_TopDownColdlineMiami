using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum BossAttackKind
{
    AimedBurst, // En salve skud mod spilleren
    BulletRing, // Ring af kugler i alle retninger
    Charge,     // Løber ind i spilleren - og bliver bagefter stunnet
    Summon      // Kalder på hjælp
}

// Ét angreb bossen kan lave. Kun felterne for den valgte Kind bruges.
[System.Serializable]
public class BossAttack
{
    public string name = "Angreb";
    public BossAttackKind kind = BossAttackKind.AimedBurst;
    public float telegraphTime = 0.6f;      // Advarsel før angrebet (bossen blinker)
    public float recoverTime = 0.5f;        // Pause efter angrebet

    [Header("AimedBurst / BulletRing")]
    public WeaponData weapon;               // Bruges til kuglerne
    public int count = 5;                   // Burst: antal skud. Ring: kugler pr. ring
    public int repeats = 1;                 // Ring: antal ringe (hver drejet lidt)
    public float interval = 0.15f;          // Tid mellem skud / ringe

    [Header("Charge")]
    public float chargeSpeed = 9f;
    public float chargeDuration = 0.8f;
    public float stunTime = 2f;             // Bossen er stunnet (og sårbar) bagefter

    [Header("Summon")]
    public GameObject summonPrefab;         // Fx en fjende-prefab
    public int summonCount = 2;
    public float summonRadius = 2.5f;
}

// En fase: bossen skifter fase når dens liv falder under "Starts Below Health".
[System.Serializable]
public class BossPhase
{
    public string name = "Fase 1";
    [Range(0f, 1f)] public float startsBelowHealth = 1f; // Første fase = 1. Næste fx 0.5 (halvt liv)
    public float moveSpeed = 2f;
    public float timeBetweenAttacks = 2f;
    public bool randomAttackOrder = true;
    public List<BossAttack> attacks = new List<BossAttack>();
    public UnityEvent onPhaseStart;
}

// Lægges på bossen. Tilføjer automatisk Rigidbody2D og Health (sæt Max Health højt og "Score On Death" stort).
// Bossen venter til spilleren kommer tæt på (eller skyder den), og kører så sine faser.
// Boss-dør: træk Door.Close og Door.Lock ind i "On Fight Start", og Door.Unlock og Door.Open ind i "On Defeated".
// Livsbjælke: træk et Image ind i Health -> Health Bar, og din bjælke-gruppe ind i "Health Bar Root".
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class BossAI : MonoBehaviour
{
    [Header("Start kampen")]
    public float activationRange = 8f;      // Kampen starter når spilleren er så tæt på (0 = kun hvis bossen bliver ramt)
    public float firstAttackDelay = 1f;

    [Header("Faser")]
    public List<BossPhase> phases = new List<BossPhase>();

    [Header("Bevægelse")]
    public float preferredDistance = 4f;    // Bossen holder sig cirka her fra spilleren og cirkler rundt
    public float strafeSwitchTime = 2.5f;

    [Header("Skade")]
    public int contactDamage = 1;           // Skade hvis spilleren rører bossen
    public float contactCooldown = 0.5f;

    [Header("Svagt punkt")]
    public bool onlyVulnerableWhenStunned = false; // Kan kun skades når den er stunnet (efter et Charge-angreb!)
    public GameObject vulnerableIcon;       // Fx stjerner over hovedet - vises når den er sårbar

    [Header("Visning (må gerne være tomme)")]
    public Transform muzzle;                // Hvor aimed-skud starter. Hvis tom: lidt foran bossen
    public float bulletSpawnDistance = 1f;
    public Color telegraphColor = Color.yellow;
    public GameObject healthBarRoot;        // Vises først når kampen starter

    [Header("Når den dør")]
    public bool killMinionsOnDeath = true;
    public int maxAliveMinions = 6;

    [Header("Events")]
    public UnityEvent onFightStart;
    public UnityEvent onDefeated;

    public Vector2 FacingDirection { get; private set; } = Vector2.right; // Bruges af Shield
    public bool FightStarted => fightStarted;

    private Rigidbody2D rb;
    private Health health;
    private SpriteRenderer spriteRenderer;
    private Color startColor = Color.white;
    private Transform player;

    private bool fightStarted = false;
    private bool busy = false;
    private bool charging = false;
    private bool chargeHitWall = false;
    private int phaseIndex = -1;
    private int attackIndex = 0;
    private float attackTimer = 0f;
    private float strafeTimer = 0f;
    private float strafeDirection = 1f;
    private float lastContactTime = -999f;
    private Vector2 velocity;
    private readonly List<GameObject> minions = new List<GameObject>();

    // Fylder to standard-faser ud når scriptet tilføjes, så man kan se hvordan det hænger sammen
    void Reset()
    {
        BossAttack burst = new BossAttack { name = "Salve", kind = BossAttackKind.AimedBurst, count = 5, interval = 0.15f };
        BossAttack ring = new BossAttack { name = "Ring", kind = BossAttackKind.BulletRing, count = 12, repeats = 3, interval = 0.4f };
        BossAttack charge = new BossAttack { name = "Charge", kind = BossAttackKind.Charge, telegraphTime = 0.8f };
        BossAttack summon = new BossAttack { name = "Hjælp", kind = BossAttackKind.Summon, summonCount = 2 };

        phases = new List<BossPhase>
        {
            new BossPhase { name = "Fase 1", startsBelowHealth = 1f, moveSpeed = 2f, timeBetweenAttacks = 2f, attacks = new List<BossAttack> { burst, ring } },
            new BossPhase { name = "Fase 2", startsBelowHealth = 0.5f, moveSpeed = 3f, timeBetweenAttacks = 1.2f, attacks = new List<BossAttack> { burst, ring, charge, summon } }
        };
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) startColor = spriteRenderer.color;

        if (phases.Count == 0)
        {
            Debug.LogWarning("Bossen har ingen faser - tilføj mindst én under Phases", this);
        }

        if (healthBarRoot != null) healthBarRoot.SetActive(false);
        if (vulnerableIcon != null) vulnerableIcon.SetActive(false);

        health.Invulnerable = onlyVulnerableWhenStunned;
        health.onDamaged.AddListener(OnDamaged);
        health.onDeath.AddListener(OnDeath);
    }

    void Update()
    {
        if (health.IsDead || phases.Count == 0) return;

        if (player == null) FindPlayer();
        bool playerAlive = player != null && player.gameObject.activeInHierarchy;

        if (!fightStarted)
        {
            velocity = Vector2.zero;
            if (playerAlive && activationRange > 0f && Vector2.Distance(transform.position, player.position) <= activationRange)
            {
                StartFight();
            }
            return;
        }

        if (!playerAlive)
        {
            velocity = Vector2.zero;
            return;
        }

        UpdatePhase();

        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        if (toPlayer.sqrMagnitude > 0.0001f) FacingDirection = toPlayer.normalized;

        if (busy) return; // Et angreb styrer bossen

        Move(toPlayer);

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            StartNextAttack();
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = velocity;
    }

    // ---------- Kampen ----------

    void StartFight()
    {
        if (fightStarted) return;

        fightStarted = true;
        attackTimer = firstAttackDelay;
        if (healthBarRoot != null) healthBarRoot.SetActive(true);
        onFightStart.Invoke();
    }

    void UpdatePhase()
    {
        float healthFraction = (float)health.CurrentHealth / health.maxHealth;

        int target = phaseIndex;
        for (int i = 0; i < phases.Count; i++)
        {
            if (healthFraction <= phases[i].startsBelowHealth) target = i;
        }

        if (target != phaseIndex)
        {
            phaseIndex = target;
            attackIndex = 0;
            phases[phaseIndex].onPhaseStart.Invoke();
        }
    }

    void Move(Vector2 toPlayer)
    {
        float distance = toPlayer.magnitude;
        if (distance < 0.01f)
        {
            velocity = Vector2.zero;
            return;
        }

        Vector2 direction = toPlayer / distance;

        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0f)
        {
            strafeTimer = strafeSwitchTime;
            strafeDirection = Random.value < 0.5f ? -1f : 1f;
        }

        Vector2 move;
        if (distance > preferredDistance + 1f) move = direction;
        else if (distance < preferredDistance - 1f) move = -direction;
        else move = new Vector2(-direction.y, direction.x) * strafeDirection * 0.6f; // Cirkler rundt om spilleren

        velocity = move * phases[phaseIndex].moveSpeed;
    }

    void StartNextAttack()
    {
        BossPhase phase = phases[phaseIndex];
        if (phase.attacks.Count == 0)
        {
            attackTimer = phase.timeBetweenAttacks;
            return;
        }

        BossAttack attack = phase.randomAttackOrder
            ? phase.attacks[Random.Range(0, phase.attacks.Count)]
            : phase.attacks[attackIndex++ % phase.attacks.Count];

        StartCoroutine(RunAttack(attack, phase));
    }

    // ---------- Angreb ----------

    IEnumerator RunAttack(BossAttack attack, BossPhase phase)
    {
        busy = true;
        velocity = Vector2.zero;

        yield return Telegraph(attack.telegraphTime);

        switch (attack.kind)
        {
            case BossAttackKind.AimedBurst: yield return AimedBurst(attack); break;
            case BossAttackKind.BulletRing: yield return BulletRing(attack); break;
            case BossAttackKind.Charge: yield return Charge(attack); break;
            case BossAttackKind.Summon: SummonMinions(attack); break;
        }

        yield return new WaitForSeconds(attack.recoverTime);

        busy = false;
        attackTimer = phase.timeBetweenAttacks;
    }

    // Bossen blinker så spilleren kan nå at reagere
    IEnumerator Telegraph(float time)
    {
        if (time <= 0f) yield break;

        SetTint(telegraphColor);
        yield return new WaitForSeconds(time);
        SetTint(startColor);
    }

    IEnumerator AimedBurst(BossAttack attack)
    {
        for (int i = 0; i < attack.count; i++)
        {
            if (!PlayerAlive()) yield break;

            Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
            float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

            Vector2 origin = muzzle != null ? (Vector2)muzzle.position : (Vector2)transform.position + toPlayer.normalized * bulletSpawnDistance;
            Fire(attack.weapon, origin, angle, toPlayer.magnitude);

            yield return new WaitForSeconds(attack.interval);
        }
    }

    IEnumerator BulletRing(BossAttack attack)
    {
        int bullets = Mathf.Max(attack.count, 1);
        float step = 360f / bullets;

        for (int ring = 0; ring < Mathf.Max(attack.repeats, 1); ring++)
        {
            // Hver ring drejes en halv kugle-afstand, så man kan finde hullerne
            float offset = ring * step * 0.5f;

            for (int i = 0; i < bullets; i++)
            {
                float angle = offset + i * step;
                Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                Fire(attack.weapon, (Vector2)transform.position + direction * bulletSpawnDistance, angle, -1f);
            }

            yield return new WaitForSeconds(attack.interval);
        }
    }

    IEnumerator Charge(BossAttack attack)
    {
        if (!PlayerAlive()) yield break;

        // Retningen låses - så spilleren kan nå at flytte sig
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;

        charging = true;
        chargeHitWall = false;

        float timer = 0f;
        while (timer < attack.chargeDuration && !chargeHitWall)
        {
            velocity = direction * attack.chargeSpeed;
            timer += Time.deltaTime;
            yield return null;
        }

        velocity = Vector2.zero;
        charging = false;

        // Stunnet: står stille og er sårbar
        if (attack.stunTime > 0f)
        {
            SetVulnerable(true);
            yield return new WaitForSeconds(attack.stunTime);
            SetVulnerable(false);
        }
    }

    void SummonMinions(BossAttack attack)
    {
        if (attack.summonPrefab == null) return;

        minions.RemoveAll(m => m == null);
        int room = maxAliveMinions - minions.Count;

        for (int i = 0; i < Mathf.Min(attack.summonCount, room); i++)
        {
            Vector2 position = (Vector2)transform.position + Random.insideUnitCircle.normalized * attack.summonRadius;
            GameObject minion = Instantiate(attack.summonPrefab, position, Quaternion.identity);
            minions.Add(minion);

            // De ved med det samme hvor spilleren er
            EnemyAI ai = minion.GetComponent<EnemyAI>();
            if (ai != null && PlayerAlive()) ai.Alert(player.position);
        }
    }

    void Fire(WeaponData weapon, Vector2 origin, float angle, float targetDistance)
    {
        if (weapon == null)
        {
            Debug.LogWarning("Boss-angrebet mangler et våben (Weapon)", this);
            return;
        }

        weapon.Shoot(origin, angle, gameObject, 0f, targetDistance);
    }

    // ---------- Skade og død ----------

    // Rører man bossen, tager man skade
    void OnCollisionStay2D(Collision2D collision)
    {
        if (contactDamage <= 0 || Time.time < lastContactTime + contactCooldown) return;

        PlayerController playerController = collision.collider.GetComponentInParent<PlayerController>();
        if (playerController == null) return;

        Health playerHealth = playerController.GetComponent<Health>();
        if (playerHealth == null) return;

        lastContactTime = Time.time;
        playerHealth.TakeDamage(contactDamage, transform.position);
    }

    // Rammer vi en væg under et charge, stopper det
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (charging && collision.collider.GetComponentInParent<PlayerController>() == null)
        {
            chargeHitWall = true;
        }
    }

    void OnDamaged()
    {
        StartFight();
    }

    void OnDeath()
    {
        StopAllCoroutines();
        velocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        busy = false;
        charging = false;
        enabled = false;

        if (healthBarRoot != null) healthBarRoot.SetActive(false);
        if (vulnerableIcon != null) vulnerableIcon.SetActive(false);

        if (killMinionsOnDeath)
        {
            foreach (GameObject minion in minions)
            {
                if (minion == null) continue;

                Health minionHealth = minion.GetComponent<Health>();
                if (minionHealth != null) minionHealth.Kill();
            }
        }

        onDefeated.Invoke();
    }

    // ---------- Hjælpere ----------

    void SetVulnerable(bool vulnerable)
    {
        health.Invulnerable = onlyVulnerableWhenStunned && !vulnerable;
        if (vulnerableIcon != null) vulnerableIcon.SetActive(vulnerable);
    }

    void SetTint(Color color)
    {
        if (spriteRenderer != null) spriteRenderer.color = color;
    }

    bool PlayerAlive()
    {
        return player != null && player.gameObject.activeInHierarchy;
    }

    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, preferredDistance);
    }
}
