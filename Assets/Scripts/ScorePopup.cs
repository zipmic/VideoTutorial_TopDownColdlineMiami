using UnityEngine;
using TMPro;

// Lægges på en lille prefab med en TextMeshPro (3D Object -> Text - TextMeshPro, ikke UI).
// Teksten svæver op og forsvinder igen. Træk prefaben ind i ScoreManagerens "Popup Prefab".
public class ScorePopup : MonoBehaviour
{
    public float riseSpeed = 1.5f;
    public float lifeTime = 0.8f;

    private TMP_Text label;
    private float timer;
    private Color startColor;

    void Awake()
    {
        label = GetComponentInChildren<TMP_Text>();
    }

    public void Show(string text, Color color)
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>();
        if (label == null) return;

        label.text = text;
        label.color = color;
        startColor = color;
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        timer += Time.deltaTime;
        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

        // Fade ud den sidste halvdel af tiden
        if (label != null)
        {
            float fade = Mathf.Clamp01((lifeTime - timer) / (lifeTime * 0.5f));
            label.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * fade);
        }
    }
}
