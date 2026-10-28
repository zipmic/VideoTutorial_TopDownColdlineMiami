using UnityEngine;
using UnityEngine.InputSystem;

// Lægges på Player (tag "Player"). Erstatter PlayerMovement.
// Tilføjer automatisk Rigidbody2D, Health og PlayerInventory.
// Kræver også en Collider2D (Gravity Scale sættes automatisk til 0).
// WASD / piletaster = gå, Space = rul.
[RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(PlayerInventory))]
public class PlayerController : MonoBehaviour
{
    [Header("Bevægelse")]
    public float moveSpeed = 5f;

    [Header("Dash (rul fremad med Space)")]
    public float dashDistance = 3f;     // Hvor langt man ruller
    public float dashDuration = 0.15f;  // Hvor lang tid rullet tager (sekunder)
    public float dashCooldown = 0.5f;   // Ventetid før man kan rulle igen
    public bool invincibleWhileDashing = true; // Man kan ikke blive ramt mens man ruller

    [Header("Retning")]
    public bool faceMouse = true;       // Sprite kigger mod musen (ellers den vej man går)

    [Header("Sprites (må gerne være tomme)")]
    public Sprite spriteUp;
    public Sprite spriteDown;
    public Sprite spriteLeft;
    public Sprite spriteRight;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Health health;
    private Camera cam;

    private Vector2 moveInput;
    private Vector2 lastDirection = Vector2.down; // Retningen vi sidst gik i
    private Vector2 facingDirection = Vector2.down;

    private bool isDashing = false;
    private Vector2 dashDirection;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;          // Top-down: ingen tyngdekraft
        rb.freezeRotation = true;      // Spilleren må ikke vælte rundt

        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
    }

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (health.IsDead) return;

        ReadInput();
        UpdateFacing();
        UpdateSprite();

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        // Tryk Space for at rulle
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryDash();
        }
    }

    void FixedUpdate()
    {
        if (health.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isDashing)
        {
            // Fart = afstand / tid
            float dashSpeed = dashDistance / dashDuration;
            rb.linearVelocity = dashDirection * dashSpeed;

            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                health.Invulnerable = false;
            }
        }
        else
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
    }

    void ReadInput()
    {
        moveInput = Vector2.zero;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    moveInput.y += 1;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  moveInput.y -= 1;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  moveInput.x -= 1;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveInput.x += 1;

        // Så man ikke går hurtigere på skrå
        moveInput = moveInput.normalized;

        if (moveInput != Vector2.zero)
        {
            lastDirection = moveInput;
        }
    }

    void UpdateFacing()
    {
        facingDirection = lastDirection;

        if (!faceMouse || cam == null || Mouse.current == null) return;

        Vector3 mousePos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 toMouse = (Vector2)mousePos - (Vector2)transform.position;
        if (toMouse.sqrMagnitude > 0.0001f)
        {
            facingDirection = toMouse.normalized;
        }
    }

    void TryDash()
    {
        if (isDashing || dashCooldownTimer > 0f) return;

        // Rul i den retning man går - eller den man sidst gik i
        dashDirection = moveInput != Vector2.zero ? moveInput : lastDirection;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        if (invincibleWhileDashing)
        {
            health.Invulnerable = true;
        }
    }

    void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        Sprite newSprite;

        // Vælg sprite ud fra den retning der "fylder mest"
        if (Mathf.Abs(facingDirection.x) > Mathf.Abs(facingDirection.y))
        {
            newSprite = facingDirection.x > 0 ? spriteRight : spriteLeft;
        }
        else
        {
            newSprite = facingDirection.y > 0 ? spriteUp : spriteDown;
        }

        // Null check: hvis der ikke er sat grafik ind, beholder vi den nuværende
        if (newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
        }
    }

    public bool IsDashing()
    {
        return isDashing;
    }
}
