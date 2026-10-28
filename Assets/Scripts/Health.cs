using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum DeathAction
{
    Destroy,    // Objektet fjernes (fjender, kasser)
    Deactivate, // Objektet slås fra (spilleren)
    Nothing     // Der sker ikke noget - brug fx "On Death" eventet
}

// Lægges på ALT der kan tage skade: spiller, fjender, kasser, tønder...
// Kuglerne (Bullet) og nærkamp (WeaponData) leder efter dette script.
public class Health : MonoBehaviour
{
    [Header("Liv")]
    public int maxHealth = 3;

    [Header("Usårlig efter skade (0 = ikke usårlig)")]
    public float invincibleTime = 0f;   // Fx 1 for spilleren
    public float blinkInterval = 0.08f; // Hvor hurtigt det blinker mens man er usårlig

    [Header("Når den bliver ramt")]
    public Color hitColor = Color.red;
    public float hitFlashTime = 0.1f;
    public SoundEffect hurtSound;       // Lyd når den bliver ramt (men ikke dør)

    [Header("Når den dør")]
    public DeathAction deathAction = DeathAction.Destroy;
    public GameObject deathPrefab;      // Fx blodpøl eller lig - bliver liggende
    public GameObject dropPrefab;       // Fx en Pickup (ammo, health...)
    [Range(0f, 1f)] public float dropChance = 1f;
    public bool restartSceneOnDeath = false; // Genstarter banen med det samme (brug på spilleren)
    public SoundEffect deathSound;      // Lyd når den dør - spiller færdig selvom objektet fjernes
    public int scoreOnDeath = 0;        // Point (før combo) når den dør - kræver en ScoreManager i banen

    [Header("UI (må gerne være tom)")]
    public Image healthBar; // Image med Image Type = Filled, Fill Method = Horizontal

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDeath;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsFull => CurrentHealth >= maxHealth;

    // Sættes udefra, fx af PlayerController mens man ruller
    public bool Invulnerable { get; set; }

    private float invincibleUntil = 0f;
    private SpriteRenderer[] spriteRenderers;
    private Color[] startColors;
    private Coroutine effectRoutine;
    private Shield shield;

    void Awake()
    {
        CurrentHealth = maxHealth;
        shield = GetComponent<Shield>();

        // Alle sprites - også fx et våben på spilleren - så det hele blinker
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        startColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            startColors[i] = spriteRenderers[i].color;
        }
    }

    void Start()
    {
        UpdateHealthBar();
    }

    public void TakeDamage(int amount)
    {
        ApplyDamage(amount);
    }

    // Med en position: et Shield kan blokere skaden hvis den kommer forfra
    public void TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (IsDead || amount <= 0) return;

        if (shield != null && shield.Blocks(sourcePosition))
        {
            shield.OnBlocked(sourcePosition);
            return;
        }

        ApplyDamage(amount);
    }

    void ApplyDamage(int amount)
    {
        if (IsDead || amount <= 0) return;
        if (Invulnerable || Time.time < invincibleUntil) return;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        UpdateHealthBar();

        if (CurrentHealth <= 0)
        {
            Die();
            return;
        }

        invincibleUntil = Time.time + invincibleTime;
        hurtSound.Play(transform.position);
        onDamaged.Invoke();

        if (isActiveAndEnabled)
        {
            StopEffect();
            effectRoutine = StartCoroutine(DamageEffect());
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;

        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        UpdateHealthBar();
    }

    // Kan kaldes fra et UnityEvent, fx en fælde
    public void Kill()
    {
        if (IsDead) return;

        CurrentHealth = 0;
        UpdateHealthBar();
        Die();
    }

    IEnumerator DamageEffect()
    {
        SetColor(hitColor);
        yield return new WaitForSeconds(hitFlashTime);
        RestoreColors();

        // Blink mens man er usårlig
        float timer = hitFlashTime;
        while (timer < invincibleTime)
        {
            SetSpritesVisible(false);
            yield return new WaitForSeconds(blinkInterval);
            SetSpritesVisible(true);
            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval * 2f;
        }

        effectRoutine = null;
    }

    void StopEffect()
    {
        if (effectRoutine != null)
        {
            StopCoroutine(effectRoutine);
            effectRoutine = null;
        }

        RestoreColors();
        SetSpritesVisible(true);
    }

    void Die()
    {
        IsDead = true;
        StopEffect();
        deathSound.Play(transform.position);
        onDeath.Invoke();

        if (scoreOnDeath > 0 && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddKill(scoreOnDeath, transform.position);
        }

        if (deathPrefab != null)
        {
            Instantiate(deathPrefab, transform.position, transform.rotation);
        }

        if (dropPrefab != null && Random.value <= dropChance)
        {
            Instantiate(dropPrefab, transform.position, Quaternion.identity);
        }

        if (restartSceneOnDeath)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        switch (deathAction)
        {
            case DeathAction.Destroy:
                Destroy(gameObject);
                break;
            case DeathAction.Deactivate:
                gameObject.SetActive(false);
                break;
        }
    }

    void SetColor(Color color)
    {
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            if (sr != null) sr.color = color;
        }
    }

    void RestoreColors()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null) spriteRenderers[i].color = startColors[i];
        }
    }

    void SetSpritesVisible(bool visible)
    {
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            if (sr != null) sr.enabled = visible;
        }
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            // fillAmount går fra 0 (tom) til 1 (fuld)
            healthBar.fillAmount = (float)CurrentHealth / maxHealth;
        }
    }
}
