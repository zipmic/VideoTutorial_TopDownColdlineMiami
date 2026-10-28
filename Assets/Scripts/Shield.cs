using UnityEngine;
using UnityEngine.Events;

// Lægges på en fjende (ved siden af Health) for at give den et skjold.
// Skud og slag forfra bliver blokeret. Bagfra og eksplosioner (granater, tønder) går igennem.
public class Shield : MonoBehaviour
{
    [Range(30f, 360f)] public float blockAngle = 160f;  // Hvor bredt skjoldet dækker foran fjenden (grader)

    [Header("Effekter (må gerne være tomme)")]
    public GameObject blockEffectPrefab;    // Fx gnister
    public float effectLifetime = 1f;
    public SoundEffect blockSound;
    public UnityEvent onBlocked;

    private EnemyAI enemy;
    private BossAI boss;

    void Awake()
    {
        enemy = GetComponent<EnemyAI>();
        boss = GetComponent<BossAI>();
    }

    Vector2 Forward()
    {
        if (enemy != null) return enemy.FacingDirection;
        if (boss != null) return boss.FacingDirection;
        return transform.right;
    }

    // Blokerer skaden hvis den kommer forfra
    public bool Blocks(Vector2 sourcePosition)
    {
        Vector2 toSource = sourcePosition - (Vector2)transform.position;
        if (toSource.sqrMagnitude < 0.0001f) return true;

        return Vector2.Angle(Forward(), toSource) <= blockAngle * 0.5f;
    }

    public void OnBlocked(Vector2 sourcePosition)
    {
        Vector2 toSource = (sourcePosition - (Vector2)transform.position).normalized;
        Vector2 effectPosition = (Vector2)transform.position + toSource * 0.6f;

        if (blockEffectPrefab != null)
        {
            GameObject effect = Instantiate(blockEffectPrefab, effectPosition, Quaternion.identity);
            Destroy(effect, effectLifetime);
        }

        blockSound.Play(effectPosition);

        onBlocked.Invoke();
    }

    void OnDrawGizmosSelected()
    {
        float angle = enemy != null && Application.isPlaying ? Mathf.Atan2(enemy.FacingDirection.y, enemy.FacingDirection.x) * Mathf.Rad2Deg : 0f;
        float half = blockAngle * 0.5f;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0f, 0f, angle - half) * Vector3.right);
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0f, 0f, angle + half) * Vector3.right);
    }
}
