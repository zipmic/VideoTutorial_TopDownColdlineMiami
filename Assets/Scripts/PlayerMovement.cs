using UnityEngine;
using UnityEngine.InputSystem;

// Lægges på Player.
// Kræver: Rigidbody2D (Gravity Scale = 0) og en Collider2D.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Bevægelse")]
    public float moveSpeed = 5f;

    [Header("Dash (rul fremad med Space)")]
    public float dashDistance = 3f;     // Hvor langt man ruller
    public float dashDuration = 0.15f;  // Hvor lang tid rullet tager (sekunder)
    public float dashCooldown = 0.5f;   // Ventetid før man kan rulle igen

    [Header("Sprites (må gerne være tomme)")]
    public Sprite spriteUp;
    public Sprite spriteDown;
    public Sprite spriteLeft;
    public Sprite spriteRight;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private Vector2 moveInput;
    private Vector2 lastDirection = Vector2.down; // Retningen vi sidst gik i

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
    }

    void Update()
    {
        ReadInput();
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
        if (isDashing)
        {
            // Fart = afstand / tid
            float dashSpeed = dashDistance / dashDuration;
            rb.linearVelocity = dashDirection * dashSpeed;

            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
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

    void TryDash()
    {
        if (isDashing || dashCooldownTimer > 0f) return;

        // Rul i den retning man går - eller den man sidst gik i
        dashDirection = moveInput != Vector2.zero ? moveInput : lastDirection;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
    }

    void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        Sprite newSprite;

        // Vælg sprite ud fra den retning der "fylder mest"
        if (Mathf.Abs(lastDirection.x) > Mathf.Abs(lastDirection.y))
        {
            newSprite = lastDirection.x > 0 ? spriteRight : spriteLeft;
        }
        else
        {
            newSprite = lastDirection.y > 0 ? spriteUp : spriteDown;
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
