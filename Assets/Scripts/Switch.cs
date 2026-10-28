using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public enum SwitchType
{
    Button,         // Spilleren trykker på en tast tæt på
    PressurePlate   // Aktiv mens nogen står på den
}

// Lægges på en knap eller trykplade. Kræver en Collider2D (Is Trigger sættes automatisk).
// Træk dørene ind i "On Activated" / "On Deactivated" og vælg fx Door -> Open / Close / Toggle.
// Kan også aktivere alt andet: fælder, alarmer, spawn af fjender...
[RequireComponent(typeof(Collider2D))]
public class Switch : MonoBehaviour
{
    [Header("Type")]
    public SwitchType type = SwitchType.Button;

    [Header("Knap")]
    public Key interactKey = Key.E;
    public bool toggle = true;          // true: tænd/sluk (kalder både Activated og Deactivated). false: kalder kun Activated hver gang
    public GameObject interactPrompt;   // Fx et "E" - vises når spilleren står ved knappen

    [Header("Trykplade")]
    public bool staysPressed = false;   // Bliver nede selv efter man går af
    public bool playerActivates = true;
    public bool enemiesActivate = false;

    [Header("Regler")]
    public bool oneTimeOnly = false;    // Kan kun bruges én gang

    [Header("Udseende (må gerne være tomme)")]
    public Sprite offSprite;
    public Sprite onSprite;
    public AudioClip switchSound;

    [Header("Events")]
    public UnityEvent onActivated;
    public UnityEvent onDeactivated;

    public bool IsOn { get; private set; }

    private SpriteRenderer spriteRenderer;
    private bool playerInside = false;
    private bool used = false;
    private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisuals();
    }

    void Update()
    {
        if (type == SwitchType.Button)
        {
            if (playerInside && Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
            {
                Press();
            }
        }
        else
        {
            // Ryd op hvis noget på pladsen er blevet fjernet (fx en fjende der døde)
            occupants.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);

            if (occupants.Count == 0 && IsOn && !staysPressed && !used)
            {
                SetOn(false);
            }
        }
    }

    // ---------- Kaldes fra andre UnityEvents ----------

    public void Activate()
    {
        SetOn(true);
    }

    public void Deactivate()
    {
        SetOn(false);
    }

    // ---------- Trigger ----------

    void OnTriggerEnter2D(Collider2D other)
    {
        if (type == SwitchType.Button)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;

            playerInside = true;
            if (interactPrompt != null) interactPrompt.SetActive(true);
        }
        else if (IsActivator(other))
        {
            occupants.Add(other);

            if (!IsOn && !used)
            {
                SetOn(true);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (type == SwitchType.Button)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;

            playerInside = false;
            if (interactPrompt != null) interactPrompt.SetActive(false);
        }
        else
        {
            occupants.Remove(other);
        }
    }

    bool IsActivator(Collider2D other)
    {
        if (playerActivates && other.GetComponentInParent<PlayerController>() != null) return true;
        if (enemiesActivate && other.GetComponentInParent<EnemyAI>() != null) return true;
        return false;
    }

    // ---------- Skift tilstand ----------

    void Press()
    {
        if (used) return;

        if (toggle)
        {
            SetOn(!IsOn);
        }
        else
        {
            PlaySound();
            onActivated.Invoke();
            if (oneTimeOnly) used = true;
        }
    }

    void SetOn(bool on)
    {
        if (IsOn == on || used) return;

        IsOn = on;
        UpdateVisuals();
        PlaySound();

        if (on) onActivated.Invoke();
        else onDeactivated.Invoke();

        if (oneTimeOnly && on)
        {
            used = true;
            if (interactPrompt != null) interactPrompt.SetActive(false);
        }
    }

    void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        Sprite sprite = IsOn ? onSprite : offSprite;
        if (sprite != null) spriteRenderer.sprite = sprite;
    }

    void PlaySound()
    {
        if (switchSound != null)
        {
            AudioSource.PlayClipAtPoint(switchSound, transform.position);
        }
    }
}
