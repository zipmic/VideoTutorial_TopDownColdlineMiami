using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public enum DoorStyle
{
    Slide,      // Glider til siden
    Swing,      // Svinger om dørens pivot-punkt (sæt sprite-pivot ved hængslet)
    Disappear   // Forsvinder helt
}

public enum DoorOpensBy
{
    Automatic,      // Åbner når man kommer tæt på
    InteractKey,    // Åbner når man trykker på tasten tæt på
    OnlyByEvents    // Åbner kun fra en Switch / andet UnityEvent
}

public enum DoorLock
{
    Unlocked,
    NeedsKey,               // Spilleren skal have en nøgle med samme Key Id
    LockedUntilTriggered    // Kan kun åbnes af en Switch / andet UnityEvent (eller Unlock)
}

// Lægges på dør-objektet (sprite + Collider2D der IKKE er trigger). Læg den gerne på laget "Walls",
// så fjender ikke kan se igennem en lukket dør.
// Åbner man den, slås collideren fra, og døren animeres. Kald Open/Close/Toggle/Unlock fra en Switch.
public class Door : MonoBehaviour
{
    [Header("Udseende")]
    public DoorStyle style = DoorStyle.Slide;
    public Vector2 slideOffset = new Vector2(1.5f, 0f); // Hvor langt den glider (i forhold til dens forælder)
    public float swingAngle = 90f;
    public float openTime = 0.25f;

    [Header("Hvordan åbnes den?")]
    public DoorOpensBy opensBy = DoorOpensBy.InteractKey;
    public Key interactKey = Key.E;
    public float interactRadius = 1.5f;
    public bool enemiesCanOpen = true;      // Automatic: fjender åbner ulåste døre
    public bool closeWhenLeft = false;      // Automatic: lukker igen når ingen er tæt på
    public float closeDelay = 0.5f;

    [Header("Lås")]
    public DoorLock lockMode = DoorLock.Unlocked;
    public string requiredKeyId = "red";    // Bruges ved NeedsKey
    public bool consumeKey = false;         // Nøglen forsvinder når den er brugt

    [Header("Start")]
    public bool startOpen = false;

    [Header("Lyd (må gerne være tomme)")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound;

    [Header("Events")]
    public UnityEvent onOpened;
    public UnityEvent onClosed;
    public UnityEvent onLockedAttempt;

    public bool IsOpen { get; private set; }

    private Collider2D[] colliders;
    private SpriteRenderer[] renderers;
    private Vector3 closedPosition;
    private Quaternion closedRotation;
    private float openAmount = 0f;      // 0 = lukket, 1 = åben
    private float checkTimer = 0f;
    private float closeTimer = 0f;
    private float lockedFeedbackTimer = 0f;

    void Awake()
    {
        colliders = GetComponentsInChildren<Collider2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        closedPosition = transform.localPosition;
        closedRotation = transform.localRotation;
    }

    void Start()
    {
        if (startOpen)
        {
            IsOpen = true;
            openAmount = 1f;
            ApplyOpenAmount();
        }
    }

    void Update()
    {
        if (lockedFeedbackTimer > 0f) lockedFeedbackTimer -= Time.deltaTime;

        HandleOpener();
        Animate();
    }

    // ---------- Kaldes fra Switch / UnityEvents ----------

    public void Open()
    {
        if (IsOpen) return;

        IsOpen = true;
        PlaySound(openSound);
        onOpened.Invoke();
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        PlaySound(closeSound);
        onClosed.Invoke();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Unlock()
    {
        lockMode = DoorLock.Unlocked;
    }

    public void Lock()
    {
        lockMode = DoorLock.LockedUntilTriggered;
    }

    // ---------- Spiller / fjender ----------

    void HandleOpener()
    {
        switch (opensBy)
        {
            case DoorOpensBy.InteractKey:
                if (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
                {
                    GameObject opener = FindOpenerNearby(false);
                    if (opener != null)
                    {
                        if (IsOpen) Close();
                        else TryOpen(opener);
                    }
                }
                break;

            case DoorOpensBy.Automatic:
                // Tjek kun ti gange i sekundet - ikke hver frame
                checkTimer -= Time.deltaTime;
                if (checkTimer > 0f) return;
                checkTimer = 0.1f;

                GameObject nearby = FindOpenerNearby(enemiesCanOpen);
                if (nearby != null)
                {
                    closeTimer = closeDelay;
                    if (!IsOpen) TryOpen(nearby);
                }
                else if (IsOpen && closeWhenLeft)
                {
                    closeTimer -= 0.1f;
                    if (closeTimer <= 0f) Close();
                }
                break;
        }
    }

    // Finder spilleren (eller en fjende) tæt på døren
    GameObject FindOpenerNearby(bool includeEnemies)
    {
        foreach (Collider2D col in Physics2D.OverlapCircleAll(transform.position, interactRadius))
        {
            PlayerController player = col.GetComponentInParent<PlayerController>();
            if (player != null) return player.gameObject;

            if (includeEnemies)
            {
                EnemyAI enemy = col.GetComponentInParent<EnemyAI>();
                if (enemy != null) return enemy.gameObject;
            }
        }

        return null;
    }

    void TryOpen(GameObject opener)
    {
        bool isPlayer = opener.GetComponent<PlayerController>() != null;

        switch (lockMode)
        {
            case DoorLock.Unlocked:
                Open();
                return;

            case DoorLock.NeedsKey:
                PlayerInventory inventory = isPlayer ? opener.GetComponent<PlayerInventory>() : null;
                if (inventory != null && inventory.HasKey(requiredKeyId))
                {
                    if (consumeKey) inventory.RemoveKey(requiredKeyId);
                    lockMode = DoorLock.Unlocked;
                    Open();
                    return;
                }
                break;
        }

        // Låst: giv besked (men ikke hver frame)
        if (lockedFeedbackTimer <= 0f)
        {
            lockedFeedbackTimer = 1f;
            PlaySound(lockedSound);
            onLockedAttempt.Invoke();
        }
    }

    // ---------- Animation ----------

    void Animate()
    {
        float target = IsOpen ? 1f : 0f;
        if (Mathf.Approximately(openAmount, target)) return;

        openAmount = Mathf.MoveTowards(openAmount, target, Time.deltaTime / Mathf.Max(openTime, 0.01f));
        ApplyOpenAmount();
    }

    void ApplyOpenAmount()
    {
        switch (style)
        {
            case DoorStyle.Slide:
                transform.localPosition = closedPosition + (Vector3)(slideOffset * openAmount);
                break;

            case DoorStyle.Swing:
                transform.localRotation = closedRotation * Quaternion.Euler(0f, 0f, swingAngle * openAmount);
                break;

            case DoorStyle.Disappear:
                foreach (SpriteRenderer sr in renderers) sr.enabled = openAmount <= 0f;
                break;
        }

        // Solid kun når helt lukket - så man kan gå igennem så snart den begynder at åbne
        bool solid = openAmount <= 0f;
        foreach (Collider2D col in colliders) col.enabled = solid;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (opensBy == DoorOpensBy.OnlyByEvents) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
