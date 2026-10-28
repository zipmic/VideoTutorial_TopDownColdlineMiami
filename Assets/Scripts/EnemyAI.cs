using UnityEngine;
using UnityEngine.Events;

// Lægges på Enemy (giv den tag "Enemy"). Erstatter Enemy + EnemyGun.
// Tilføjer automatisk Rigidbody2D og Health. Kræver en Collider2D.
//
// Fjenden går i en cirkel mellem waypoints, ser inden for en kegle, hører skud,
// jagter og skyder (eller slår) spilleren, og leder efter ham når den mister ham.
// Der er ingen "pathfinding" - fjenden går i lige linje, så sæt waypoints så den ikke skal rundt om hjørner.
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Sleeping, Patrol, Investigate, Chase }

    [Header("Start")]
    public bool startAsleep = false;
    public float wakeUpRadius = 2f;         // Vågner når spilleren kommer så tæt på (0 = kun af lyd/skade)
    public float startFacingAngle = 0f;     // 0 = højre, 90 = op, 180 = venstre, 270 = ned

    [Header("Syn")]
    public float viewDistance = 7f;         // Hvor langt fjenden kan se
    [Range(10f, 360f)] public float viewAngle = 90f; // Hvor bred synskeglen er (grader)
    public float closeRange = 1.5f;         // Ser spilleren uanset retning når han er så tæt på
    public LayerMask obstacleLayers;        // Fx "Walls" - fjenden kan ikke se igennem dem

    [Header("Hørelse")]
    public bool canHear = true;             // Hører skud og går hen for at tjekke
    public float hearingMultiplier = 1f;    // 2 = hører dobbelt så langt som våbnets støj

    [Header("Bevægelse")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 3.5f;
    public float turnSpeed = 360f;          // Grader i sekundet fjenden kan dreje

    [Header("Patrulje (tom = står stille)")]
    public Transform[] waypoints;           // Tomme GameObjects - IKKE children af fjenden
    public float waitAtWaypoint = 1f;

    [Header("Leder efter spilleren")]
    public float searchTime = 3f;           // Hvor længe den kigger rundt når den har mistet dig

    [Header("Angreb")]
    public WeaponData weapon;
    public float attackRange = 6f;          // Skyder først når spilleren er så tæt på
    public float stopDistance = 3f;         // Skytter stopper her (nærkamp går helt tæt på)
    public float reactionTime = 0.4f;       // Ventetid fra den ser dig til første skud
    public float aimTolerance = 10f;        // Hvor præcist den skal sigte før den skyder (grader)
    public float extraSpread = 3f;          // Ekstra unøjagtighed oven i våbnets egen
    public float chargeTime = 0f;           // Sniper: sigter så længe (med laser) før skuddet. 0 = skyder med det samme
    public float retreatDistance = 0f;      // Sniper/bombekaster: bakker væk hvis spilleren kommer tættere på end dette

    [Header("Alarm")]
    public bool alertOthers = true;         // Råber til andre fjender når den ser dig
    public float alertRadius = 8f;

    [Header("Visning (må gerne være tomme)")]
    public Transform gunPivot;              // Våben der drejer rundt om fjenden (child af Enemy)
    public Transform muzzle;                // Hvor kuglerne starter (child af gunPivot)
    public float distanceFromEnemy = 0.7f;
    public GameObject spottedIcon;          // Fx et "!" - vises mens den jager dig
    public GameObject suspiciousIcon;       // Fx et "?" - vises når den har hørt noget
    public LineRenderer laser;              // Sniperens laser-sigte (Use World Space slås til automatisk)
    public Sprite spriteUp;
    public Sprite spriteDown;
    public Sprite spriteLeft;
    public Sprite spriteRight;

    [Header("Events")]
    public UnityEvent onSpotPlayer;

    public State CurrentState => state;
    public Vector2 FacingDirection => FacingVector(); // Bruges af Shield

    private State state;
    private Rigidbody2D rb;
    private Health health;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer gunRenderer;
    private Vector3 gunStartScale;
    private Transform player;

    private float facingAngle;
    private Vector2 velocity;

    private int waypointIndex = 0;
    private bool waiting = false;
    private float waitTimer = 0f;

    private Vector2 lastKnownPlayerPos;
    private Vector2 investigatePos;
    private bool arrivedAtInvestigatePos = false;
    private float searchTimer = 0f;

    private float reactionTimer = 0f;
    private float shootTimer = 0f;
    private int currentAmmo;
    private bool isReloading = false;
    private float reloadTimer = 0f;
    private bool charging = false;
    private float chargeTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        facingAngle = startFacingAngle;
        state = startAsleep ? State.Sleeping : State.Patrol;
        currentAmmo = weapon != null ? weapon.magazineSize : 0;

        if (gunPivot != null)
        {
            gunStartScale = gunPivot.localScale;
            gunRenderer = gunPivot.GetComponentInChildren<SpriteRenderer>();

            if (gunRenderer != null && weapon != null && weapon.weaponSprite != null)
            {
                gunRenderer.sprite = weapon.weaponSprite;
            }
        }

        if (laser != null)
        {
            laser.useWorldSpace = true;
            laser.positionCount = 2;
            laser.enabled = false;
        }

        health.onDeath.AddListener(OnDeath);
        health.onDamaged.AddListener(OnDamaged);

        UpdateIcons();
    }

    void OnEnable()
    {
        Noise.OnNoise += HandleNoise;
    }

    void OnDisable()
    {
        Noise.OnNoise -= HandleNoise;
    }

    void Update()
    {
        if (player == null) FindPlayer();

        if (shootTimer > 0f) shootTimer -= Time.deltaTime;

        bool canSee = state != State.Sleeping && CanSeePlayer();

        if (canSee)
        {
            lastKnownPlayerPos = player.position;
            if (state != State.Chase) StartChase();
        }
        else if (state == State.Chase)
        {
            // Mistede spilleren af syne: gå hen hvor vi sidst så ham
            GoInvestigate(lastKnownPlayerPos);
        }

        velocity = Vector2.zero;
        charging = false;

        switch (state)
        {
            case State.Sleeping: UpdateSleeping(); break;
            case State.Patrol: UpdatePatrol(); break;
            case State.Investigate: UpdateInvestigate(); break;
            case State.Chase: UpdateChase(); break;
        }

        // Afbrydes sigtet, skal sniperen starte forfra
        if (!charging) chargeTimer = 0f;

        UpdateVisuals();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = velocity;
    }

    // ---------- Tilstande ----------

    void UpdateSleeping()
    {
        if (wakeUpRadius <= 0f || player == null || !player.gameObject.activeInHierarchy) return;

        if (Vector2.Distance(transform.position, player.position) <= wakeUpRadius)
        {
            state = State.Patrol;
            UpdateIcons();
        }
    }

    void UpdatePatrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[waypointIndex];
        if (target == null)
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            return;
        }

        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                waiting = false;
                waypointIndex = (waypointIndex + 1) % waypoints.Length;
            }
            return;
        }

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        if (toTarget.magnitude > 0.15f)
        {
            MoveToward(toTarget, patrolSpeed);
        }
        else
        {
            waiting = true;
            waitTimer = waitAtWaypoint;
        }
    }

    void UpdateInvestigate()
    {
        if (!arrivedAtInvestigatePos)
        {
            Vector2 toTarget = investigatePos - (Vector2)transform.position;
            if (toTarget.magnitude > 0.2f)
            {
                MoveToward(toTarget, chaseSpeed);
            }
            else
            {
                arrivedAtInvestigatePos = true;
                searchTimer = searchTime;
            }
            return;
        }

        // Fremme: kig rundt et stykke tid, og gå så tilbage til at patruljere
        facingAngle += 90f * Time.deltaTime;
        searchTimer -= Time.deltaTime;

        if (searchTimer <= 0f)
        {
            state = State.Patrol;
            UpdateIcons();
        }
    }

    void UpdateChase()
    {
        // I Chase kan vi altid se spilleren (ellers var vi skiftet til Investigate)
        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        float distance = toPlayer.magnitude;
        float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        facingAngle = Mathf.MoveTowardsAngle(facingAngle, targetAngle, turnSpeed * Time.deltaTime);
        reactionTimer += Time.deltaTime;

        bool melee = weapon != null && weapon.isMelee;
        float stop = melee ? weapon.meleeRange * 0.7f : stopDistance;
        if (retreatDistance > 0f && distance < retreatDistance)
        {
            // For tæt på: bak væk (men bliv ved med at kigge på spilleren)
            velocity = -toPlayer.normalized * chaseSpeed;
        }
        else if (distance > stop)
        {
            velocity = toPlayer.normalized * chaseSpeed;
        }

        TryAttack(distance);
    }

    void StartChase()
    {
        state = State.Chase;
        reactionTimer = 0f;
        UpdateIcons();
        onSpotPlayer.Invoke();

        if (alertOthers)
        {
            AlertNearbyEnemies();
        }
    }

    void GoInvestigate(Vector2 position)
    {
        state = State.Investigate;
        investigatePos = position;
        arrivedAtInvestigatePos = false;
        UpdateIcons();
    }

    // Kaldes af andre fjender, eller når vi bliver ramt
    public void Alert(Vector2 position)
    {
        if (health.IsDead || state == State.Chase) return;

        GoInvestigate(position);
    }

    void AlertNearbyEnemies()
    {
        foreach (Collider2D col in Physics2D.OverlapCircleAll(transform.position, alertRadius))
        {
            EnemyAI other = col.GetComponentInParent<EnemyAI>();
            if (other != null && other != this)
            {
                other.Alert(player.position);
            }
        }
    }

    // ---------- Syn, hørelse og skade ----------

    bool CanSeePlayer()
    {
        if (player == null || !player.gameObject.activeInHierarchy) return false;

        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        float distance = toPlayer.magnitude;

        // Har den allerede set dig, mister den dig først lidt længere væk
        float range = state == State.Chase ? viewDistance * 1.3f : viewDistance;
        if (distance > range) return false;

        // Uden for synskeglen? (medmindre du er helt tæt på)
        if (distance > closeRange && Vector2.Angle(FacingVector(), toPlayer) > viewAngle * 0.5f) return false;

        // Er der en væg imellem?
        RaycastHit2D hit = Physics2D.Raycast(transform.position, toPlayer.normalized, distance, obstacleLayers);
        return hit.collider == null;
    }

    void HandleNoise(Vector2 position, float radius, GameObject source)
    {
        if (!canHear || health.IsDead || state == State.Chase) return;

        // Fjender hører ikke hinandens skud - kun andres (spillerens)
        if (source != null && (source == gameObject || source.CompareTag(tag))) return;

        if (Vector2.Distance(transform.position, position) > radius * hearingMultiplier) return;

        GoInvestigate(position);
    }

    void OnDamaged()
    {
        // Bliver vi ramt, ved vi nogenlunde hvor skytten står
        if (player == null) FindPlayer();
        if (player != null && player.gameObject.activeInHierarchy)
        {
            Alert(player.position);
        }
    }

    void OnDeath()
    {
        enabled = false; // Slår også Update og hørelse fra
        rb.linearVelocity = Vector2.zero;

        if (spottedIcon != null) spottedIcon.SetActive(false);
        if (suspiciousIcon != null) suspiciousIcon.SetActive(false);
        if (laser != null) laser.enabled = false;
    }

    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
    }

    // ---------- Angreb ----------

    void TryAttack(float distance)
    {
        if (weapon == null) return;

        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0f)
            {
                currentAmmo = weapon.magazineSize;
                isReloading = false;
            }
            return;
        }

        if (reactionTimer < reactionTime || shootTimer > 0f) return;

        if (weapon.isMelee)
        {
            if (distance <= weapon.meleeRange)
            {
                weapon.MeleeAttack(transform.position, facingAngle, gameObject);
                shootTimer = weapon.shootInterval;
            }
            return;
        }

        if (distance > attackRange) return;

        // Sigter fjenden nogenlunde rigtigt?
        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        if (Mathf.Abs(Mathf.DeltaAngle(facingAngle, targetAngle)) > aimTolerance) return;

        if (currentAmmo <= 0)
        {
            isReloading = true;
            reloadTimer = weapon.reloadTime;
            return;
        }

        Vector2 origin = muzzle != null ? (Vector2)muzzle.position : (Vector2)transform.position + FacingVector() * distanceFromEnemy;

        // Sniper: sigter med laser et stykke tid før skuddet, så spilleren kan nå at flytte sig
        if (chargeTime > 0f)
        {
            charging = true;
            chargeTimer += Time.deltaTime;
            UpdateLaser(origin);

            if (chargeTimer < chargeTime) return;

            charging = false;
            chargeTimer = 0f;
        }

        weapon.Shoot(origin, facingAngle, gameObject, extraSpread, distance);

        currentAmmo--;
        shootTimer = weapon.shootInterval;
    }

    void UpdateLaser(Vector2 origin)
    {
        if (laser == null) return;

        Vector2 direction = FacingVector();
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, attackRange, obstacleLayers);
        Vector2 end = hit.collider != null ? hit.point : origin + direction * attackRange;

        laser.SetPosition(0, origin);
        laser.SetPosition(1, end);
    }

    // ---------- Hjælpere ----------

    void MoveToward(Vector2 direction, float speed)
    {
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        facingAngle = Mathf.MoveTowardsAngle(facingAngle, targetAngle, turnSpeed * Time.deltaTime);
        velocity = direction.normalized * speed;
    }

    Vector2 FacingVector()
    {
        return new Vector2(Mathf.Cos(facingAngle * Mathf.Deg2Rad), Mathf.Sin(facingAngle * Mathf.Deg2Rad));
    }

    void UpdateIcons()
    {
        if (spottedIcon != null) spottedIcon.SetActive(state == State.Chase);
        if (suspiciousIcon != null) suspiciousIcon.SetActive(state == State.Investigate);
    }

    void UpdateVisuals()
    {
        Vector2 facing = FacingVector();

        if (laser != null) laser.enabled = charging;

        if (spriteRenderer != null)
        {
            Sprite newSprite;
            if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y))
            {
                newSprite = facing.x > 0 ? spriteRight : spriteLeft;
            }
            else
            {
                newSprite = facing.y > 0 ? spriteUp : spriteDown;
            }

            // Hvis der ikke er sat grafik ind, beholder vi den nuværende
            if (newSprite != null) spriteRenderer.sprite = newSprite;
        }

        if (gunPivot != null)
        {
            gunPivot.position = transform.position + (Vector3)(facing * distanceFromEnemy);
            gunPivot.rotation = Quaternion.Euler(0f, 0f, facingAngle);

            // Vend våbnet på venstre side, så det ikke står på hovedet
            Vector3 scale = gunStartScale;
            scale.y = facing.x < 0 ? -Mathf.Abs(gunStartScale.y) : Mathf.Abs(gunStartScale.y);
            gunPivot.localScale = scale;
        }
    }

    // Tegner synskegle, patrulje og rækkevidde i Scene-vinduet når fjenden er valgt
    void OnDrawGizmosSelected()
    {
        float angle = Application.isPlaying ? facingAngle : startFacingAngle;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, closeRange);

        float half = viewAngle * 0.5f;
        Vector3 left = Quaternion.Euler(0f, 0f, angle - half) * Vector3.right * viewDistance;
        Vector3 right = Quaternion.Euler(0f, 0f, angle + half) * Vector3.right * viewDistance;
        Gizmos.DrawLine(transform.position, transform.position + left);
        Gizmos.DrawLine(transform.position, transform.position + right);

        Vector3 previous = transform.position + left;
        for (int i = 1; i <= 16; i++)
        {
            float a = angle - half + viewAngle * i / 16f;
            Vector3 point = transform.position + Quaternion.Euler(0f, 0f, a) * Vector3.right * viewDistance;
            Gizmos.DrawLine(previous, point);
            previous = point;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (waypoints != null && waypoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                Transform a = waypoints[i];
                Transform b = waypoints[(i + 1) % waypoints.Length];
                if (a == null || b == null) continue;

                Gizmos.DrawWireSphere(a.position, 0.15f);
                Gizmos.DrawLine(a.position, b.position);
            }
        }
    }
}
