using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Lægges på granater og eksplosive tønder.
// Tønde: giv den en Health (Death Action = Destroy) og træk Explosion -> Explode() ind i Health's "On Death".
// Granat: bruges af Grenade-scriptet.
public class Explosion : MonoBehaviour
{
    [Header("Skade")]
    public float radius = 2.5f;
    public int damage = 3;                  // Granater: bruger våbnets Damage i stedet
    public bool damageFalloff = true;       // Mindre skade jo længere væk
    public bool hurtsEveryone = false;      // Skader også den der kastede den (og dens venner)

    [Header("Effekter (må gerne være tomme)")]
    public GameObject effectPrefab;         // Fx en eksplosions-animation
    public float effectLifetime = 2f;
    public AudioClip sound;
    public float noiseRadius = 15f;         // Fjender inden for denne afstand hører det
    public UnityEvent onExploded;

    private bool exploded = false;

    // Kan kaldes direkte fra et UnityEvent
    public void Explode()
    {
        Explode(null);
    }

    // source = den der udløste den (så den ikke skader sit eget "hold")
    public void Explode(GameObject source)
    {
        if (exploded) return;
        exploded = true;

        Vector2 position = transform.position;
        HashSet<Health> alreadyHit = new HashSet<Health>();

        foreach (Collider2D col in Physics2D.OverlapCircleAll(position, radius))
        {
            Health health = col.GetComponentInParent<Health>();
            if (health == null || health.IsDead || !alreadyHit.Add(health)) continue;

            if (source != null && !hurtsEveryone && health.CompareTag(source.tag)) continue;

            int amount = damage;
            if (damageFalloff)
            {
                float distance = Vector2.Distance(position, col.ClosestPoint(position));
                float fraction = 1f - 0.7f * Mathf.Clamp01(distance / radius);
                amount = Mathf.Max(1, Mathf.RoundToInt(damage * fraction));
            }

            // Uden kilde-position: eksplosioner går forbi skjolde
            health.TakeDamage(amount);
        }

        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(effect, effectLifetime);
        }

        if (sound != null)
        {
            AudioSource.PlayClipAtPoint(sound, position);
        }

        Noise.Make(position, noiseRadius, source);
        onExploded.Invoke();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
