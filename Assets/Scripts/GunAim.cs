using UnityEngine;
using UnityEngine.InputSystem;

// Lægges på Gun (gerne som child af Player).
// Får gun til at cirkle rundt om spilleren og pege mod musen.
public class GunAim : MonoBehaviour
{
    public Transform player;          // Hvis tom, bruges parent
    public float distanceFromPlayer = 0.7f;

    private Camera cam;
    private Vector3 startScale;

    void Start()
    {
        cam = Camera.main;
        startScale = transform.localScale;

        if (player == null)
        {
            player = transform.parent;
        }
    }

    void Update()
    {
        if (player == null || cam == null || Mouse.current == null) return;

        // Find musens position i verden
        Vector3 mousePos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0f;

        // Retning fra spiller til mus
        Vector2 direction = mousePos - player.position;
        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        // Placér gun i en fast afstand fra spilleren
        transform.position = player.position + (Vector3)(direction * distanceFromPlayer);

        // Drej gun så den peger mod musen
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Vend gun på venstre side, så den ikke står på hovedet
        Vector3 scale = startScale;
        if (direction.x < 0)
        {
            scale.y = -Mathf.Abs(startScale.y);
        }
        else
        {
            scale.y = Mathf.Abs(startScale.y);
        }
        transform.localScale = scale;
    }
}
