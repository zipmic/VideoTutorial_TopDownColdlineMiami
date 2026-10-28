using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Lægges på Player.
public class PlayerHealth : MonoBehaviour
{
    [Header("Liv")]
    public int maxHealth = 5;

    [Header("Usårlig efter skade")]
    public float invincibleTime = 1f;   // Hvor længe man ikke kan tage skade
    public float blinkInterval = 0.08f; // Hvor hurtigt spilleren blinker

    [Header("Tag skade fra")]
    public string damageTag = "Enemy";  // Objekter med dette tag giver skade
    public int damageAmount = 1;

    [Header("UI (må gerne være tom)")]
    public Image healthBar; // Image med Image Type = Filled, Fill Method = Horizontal

    private int currentHealth;
    private bool isInvincible = false;
    private SpriteRenderer[] spriteRenderers;

    void Start()
    {
        currentHealth = maxHealth;

        // Finder alle sprites på spilleren - også gun - så det hele blinker
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        UpdateHealthBar();
    }

    public void TakeDamage(int amount)
    {
        if (isInvincible || currentHealth <= 0) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;
        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(InvincibleBlink());
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthBar();
    }

    IEnumerator InvincibleBlink()
    {
        isInvincible = true;
        float timer = 0f;

        while (timer < invincibleTime)
        {
            SetSpritesVisible(false);
            yield return new WaitForSeconds(blinkInterval);
            SetSpritesVisible(true);
            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval * 2f;
        }

        SetSpritesVisible(true);
        isInvincible = false;
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
            healthBar.fillAmount = (float)currentHealth / maxHealth;
        }
    }

    void Die()
    {
        Debug.Log("Player er død!");
        gameObject.SetActive(false);
    }

    // Skade når man rører en fjende (almindelig collider)
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(damageTag))
        {
            TakeDamage(damageAmount);
        }
    }

    // Skade når man rører en fjende (trigger collider)
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag(damageTag))
        {
            TakeDamage(damageAmount);
        }
    }
}
